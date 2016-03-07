using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TDI.Interception
{
    public class InterceptManager : IProxyTypeProvider
    {
        public static InterceptManager Current { get; }

        private readonly Dictionary<CreatedAsm, bool> assemblyBuilders = new Dictionary<CreatedAsm, bool>();
        private readonly ConcurrentDictionary<Type, Type> wrappers = new ConcurrentDictionary<Type, Type>();
        const MethodAttributes targetMethodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;

        private class CreatedAsm
        {
            public AssemblyBuilder AssemblyBuilder { get; set; }

            public string FileName { get; set; }
        }

        static InterceptManager()
        {
            Current = new InterceptManager();
        }

        private InterceptManager()
        {
            var probables = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(asm => asm.GetCustomAttributes().Any(att => att is InjectableAssemblyAttribute))
                        .SelectMany(asm => asm.DefinedTypes)
                        .Where(t => t.IsClass)
                        .Where(t =>
                            {
                                var attrs = t.GetCustomAttributes()
                                    .Where(attr => attr is TDIAttribute)
                                    .Concat(t.GetMembers()
                                        .SelectMany(m => m.GetCustomAttributes()
                                            .Where(att => att is TDIAttribute)))
                                    .ToList();

                                return attrs != null && attrs.Count() > 0;
                            })
                         .ToList();

            var asmName = new AssemblyName($"PreprocessedAsm");

            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);

            var modulePath = $"{asmName.Name}.dll";

            this.assemblyBuilders.Add(new CreatedAsm { AssemblyBuilder = asmBuilder, FileName = modulePath }, false);

            var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name, modulePath);

            probables.ForEach(t => this.createProxyType(moduleBuilder, t));
        }

        internal void Save()
        {
            this.assemblyBuilders.Where(kvp => !kvp.Value)
                .Select(kvp =>
                {
                    bool errored = false;
                    try
                    {
                        kvp.Key.AssemblyBuilder.Save(kvp.Key.FileName);
                    }
                    catch
                    {
                        errored = true;
                    }
                    return new { Key = kvp.Key, Errored = errored };
                })
                .ToList()
                .ForEach(k => this.assemblyBuilders[k.Key] = !k.Errored);
        }

        //intended for public virtual methods
        public Type GetProxyType(Type t)
        {
            if (this.wrappers.ContainsKey(t))
                return this.wrappers[t];

            var asmName = new AssemblyName($"{t.Name}Asm");

            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndSave);

            var modulePath = $"{asmName.Name}.dll";

            this.assemblyBuilders.Add(new CreatedAsm { AssemblyBuilder = asmBuilder, FileName = modulePath }, false);

            var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name, modulePath);

            return this.createProxyType(moduleBuilder, t);
        }

        private Type createProxyType(ModuleBuilder moduleToCreateIn, Type t)
        {
            if (this.wrappers.ContainsKey(t))
                return this.wrappers[t];

            if (!t.IsClass) throw new ArgumentException("Proxy types must be classes!", nameof(t));

            var methsWithAspectAttrs = t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                                .Where(m => m.GetCustomAttributes(true).Any(attr => attr is InterceptAttribute))
                                                .ToList();

            if (methsWithAspectAttrs == null || methsWithAspectAttrs.Count < 1)
            {
                this.wrappers.AddOrUpdate(t, t, (k, v) => v);
                return t;
            }

            if (methsWithAspectAttrs.Any(m => !m.IsVirtual)) throw new InvalidCastException("Aspect methods must be virtual!");

            var typeBuilder = moduleToCreateIn.DefineType(t.Name + "Wrapper",
                TypeAttributes.Class | TypeAttributes.Public, t);

            this.definePassthroughConstructors(typeBuilder, t);
            this.defineInterceptMethodHooks(typeBuilder, methsWithAspectAttrs);

            var wrapperType = typeBuilder.CreateType();

            this.wrappers.AddOrUpdate(t, wrapperType, (k, v) => v);
            return wrapperType;
        }

        private void definePassthroughConstructors(TypeBuilder typeBuilder, Type proxiedType)
        {
            foreach(var ctor in proxiedType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance))
            {
                this.definePassthroughConstructor(typeBuilder, ctor);
            }
        }

        private void definePassthroughConstructor(TypeBuilder typeBuilder, ConstructorInfo ctor)
        {
            var paramz = ctor.GetParameters();

            var newCtor = typeBuilder.DefineConstructor(
                ctor.Attributes, 
                ctor.CallingConvention, 
                paramz.Select(p => p.ParameterType).ToArray());

            for (var i = 0; i < paramz.Length; i++)
            {
                newCtor.DefineParameter(i + 1, paramz[i].Attributes, paramz[i].Name);
            }

            var ilBuilder = newCtor.GetILGenerator();

            ilBuilder.Emit(OpCodes.Ldarg_0);

            for (byte i = 1; i < paramz.Length + 1; i++)
            {
                ilBuilder.Emit(OpCodes.Ldarg_S, i);
            }
            ilBuilder.Emit(OpCodes.Call, ctor);
            ilBuilder.Emit(OpCodes.Nop);
            ilBuilder.Emit(OpCodes.Nop);
            ilBuilder.Emit(OpCodes.Ret);
        }


        private void defineInterceptMethodHooks(TypeBuilder typeBuilder, IEnumerable<MethodInfo> methods)
        {
            var interceptorMeth = typeof(InterceptManager).GetMethod("Intercept", BindingFlags.Public | BindingFlags.Static);
            var getTypeMeth = typeof(object).GetMethod("GetType");
            var getMethMeth = typeof(Type).GetMethod("GetMethod", new[] { typeof(string) });
            var interceptExCtor = typeof(InterceptException).GetConstructor(Type.EmptyTypes);

            foreach (var meth in methods)
            {
                this.defineInterceptMethodHook(typeBuilder, meth, getTypeMeth, getMethMeth, interceptorMeth, interceptExCtor);
            }
        }

        private void defineInterceptMethodHook(
            TypeBuilder typeBuilder,
            MethodInfo baseMeth, 
            MethodInfo getTypeMeth, 
            MethodInfo getMethMeth, 
            MethodInfo interceptorMeth,
            ConstructorInfo interceptExCtor)
        {

            var newMeth = typeBuilder.DefineMethod(baseMeth.Name,
                                            targetMethodAttributes,
                                            baseMeth.ReturnType,
                                            baseMeth.GetParameters()
                                                .Select(p => p.ParameterType)
                                                .ToArray());

            var paramz = baseMeth.GetParameters();

            for (var i = 0; i < paramz.Length; i++)
            {
                newMeth.DefineParameter(i + 1, paramz[i].Attributes, paramz[i].Name);
            }

            var ilBuilder = newMeth.GetILGenerator();

            ilBuilder.DeclareLocal(typeof(bool));
            ilBuilder.DeclareLocal(typeof(string));
            ilBuilder.Emit(OpCodes.Nop);
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Call, getTypeMeth);
            ilBuilder.Emit(OpCodes.Ldstr, baseMeth.Name);
            ilBuilder.Emit(OpCodes.Callvirt, getMethMeth);
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Call, interceptorMeth);
            ilBuilder.Emit(OpCodes.Stloc_0);
            ilBuilder.Emit(OpCodes.Ldloc_0);

            //skip exception if interceptor returned true
            var skipExLable = ilBuilder.DefineLabel();
            ilBuilder.Emit(OpCodes.Brfalse_S, skipExLable);
            ilBuilder.Emit(OpCodes.Newobj, interceptExCtor);
            ilBuilder.Emit(OpCodes.Throw);
            ilBuilder.MarkLabel(skipExLable);

            ilBuilder.Emit(OpCodes.Nop);
            ilBuilder.Emit(OpCodes.Ldarg_0);

            for (byte i = 1; i < paramz.Length + 1; i++)
            {
                ilBuilder.Emit(OpCodes.Ldarg_S, i);
            }

            ilBuilder.Emit(OpCodes.Call, baseMeth);
            if (!baseMeth.ReturnType.Equals(typeof(void)))
            {
                ilBuilder.Emit(OpCodes.Stloc_1);
                ilBuilder.Emit(OpCodes.Ldloc_1);
            }
            ilBuilder.Emit(OpCodes.Ret);
        }

        public static bool Intercept(MethodInfo meth, object instance)
        {
            if (meth == null) throw new ArgumentNullException(nameof(meth));

            return meth.GetCustomAttributes()
                .Where(m => m is InterceptAttribute)
                .Cast<InterceptAttribute>()
                .ToList()
                .Select(attr => attr.Intercept(meth, instance))
                .Any(r => r);
        }
    }

}
