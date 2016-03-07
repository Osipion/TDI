using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TDI.Interception.Tests
{

    public interface ISomething
    {
        void Something();
    }

    public class Something : ISomething
    {
        void ISomething.Something()
        {
            
        }
    }


    public class ConstructMe1
    { 
        [DefaultConstructor]
        public ConstructMe1()
        {

        }
    }

    public class ConstructMe2
    {
        private readonly ISomething something;

        [DefaultConstructor]
        public ConstructMe2(ISomething something)
        {
            this.something = something;
        }
    }

    public class LogAttribute : InterceptAttribute
    {
        public override bool Intercept(MethodInfo method, object instance)
        {
            Console.WriteLine($"{method.Name} called.");
            return false;
        }
    }

    [Singleton]
    public class Singleton1
    {
        public string Value { get; set; }

        [Log]
        public virtual void InterceptedMethod()
        {

        }
    }

    public class NoEmptyCtor
    {
        public string Name { get; set; }

        [Log]
        public virtual void TryToProxyMe()
        {
            Console.WriteLine("Proxied");
        }

        public NoEmptyCtor(string name)
        {
            this.Name = name;
        }
    }

    public class NoEmptyCtorDerived : NoEmptyCtor
    {
        public NoEmptyCtorDerived(string name) : base(name)
        { 
        }
    }
}
