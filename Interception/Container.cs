using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

using TDI.Interception;

namespace TDI
{
    public class Container
    {
        #region Singleton Impl
        private static readonly Container container;
        public static Container Current { get { return container; } }

        static Container()
        {
            container = new Container(InterceptManager.Current);
        }
        #endregion
        public IProxyTypeProvider ProxyProvider { get; }
        public Dictionary<Type, object> Singletons { get; } = new Dictionary<Type, object>();

        private Container(IProxyTypeProvider provider)
        {
            this.ProxyProvider = provider;
        }

        public T Get<T>() where T : class
        {
            return this.Get(typeof(T)) as T;
        }

        public object Get(Type t)
        {
            if (t.IsInterface) return this.getInterfaceImplementation(t);

            if (!t.IsClass) throw new ArgumentException($"The type parameter must be a reference type", nameof(t));

            var proxyType = this.ProxyProvider.GetProxyType(t);

            if (this.Singletons.ContainsKey(proxyType)) return this.Singletons[proxyType];

            var isSingleton = proxyType.GetCustomAttribute<SingletonAttribute>() != null;

            var instance = this.newInstance(proxyType, isSingleton);

            if (isSingleton) this.Singletons.Add(proxyType, instance);

            return instance;

        }

        private object getInterfaceImplementation(Type interfaceType)
        {
            var defaultImpl = interfaceType.GetCustomAttribute<DefaultImplementationAttribute>();

            Type implType;

            if(defaultImpl == null)
            {
                implType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.DefinedTypes)
                                .Where(t => t.ImplementedInterfaces.Contains(interfaceType))
                                .Where(t => t.IsClass && !(t.IsAbstract || t.IsCOMObject))
                                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                                .FirstOrDefault();

                if (implType == null) throw new ArgumentException($"No default implementation could be found for type {interfaceType.FullName}");
            }
            else
            {
                implType = defaultImpl.DefaultImplementationType;
            }

            return this.Get(implType);
        }

        private object newInstance(Type t, bool singleton)
        {
            var ctors = singleton ? 
                t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance) 
                : t.GetConstructors();

            if (ctors.Length < 1) throw new InvalidOperationException($"No public constructor found for type {t.FullName}");

            var defaultCtor = ctors.FirstOrDefault(c => c.GetCustomAttribute<DefaultConstructorAttribute>() != null);

            if(defaultCtor == null)
            {
                defaultCtor = t.GetConstructor(Type.EmptyTypes);
                if (defaultCtor == null)
                {
                    defaultCtor = ctors.OrderBy(c => c.GetParameters().Length).First();
                }
            }

            var paramz = defaultCtor.GetParameters().Select(p =>
            {
                var pType = p.ParameterType;

                if (pType.Equals(typeof(string))) return string.Empty;

                if (pType.IsArray) return Activator.CreateInstance(pType, new object[] { 0 });

                if (pType.IsValueType) return Activator.CreateInstance(p.ParameterType);

                return this.Get(p.ParameterType);
            })
            .ToArray();

            var instance = defaultCtor.Invoke(paramz);
            return instance;
        }
    }
}
