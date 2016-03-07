using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDI.Interception.Tests
{
    public class Speaker
    {
        public void SomeMethod() { }

        [SimonsSays]
        public virtual string SayHello(int param1)
        {
            Console.WriteLine("Hello!");
            return "Hello";
        }

        [SimonsSays]
        public virtual void SayBoo(string a, object b, IntPtr c, int d, string e)
        {
            Console.WriteLine("Boo!");
        }

        [SimonsSays]
        public virtual void NotSimon()
        {
            Console.WriteLine("This isn't Simon!");
        }

        [SimonsSays]
        public virtual void Nothing()
        {
            Console.WriteLine();
        }
    }

    public class TestDervied : Speaker
    {
        public override string SayHello(int param1)
        {
            if (InterceptManager.Intercept(this.GetType().GetMethod("SayHello"), this)) throw new InterceptException();
            return base.SayHello(param1);
        }

        public override void SayBoo(string a, object b, IntPtr c, int d, string e)
        {
            if (InterceptManager.Intercept(this.GetType().GetMethod("SayBoo"), this)) throw new InterceptException();
            base.SayBoo(a, b, c, d, e);
        }
    }
}
