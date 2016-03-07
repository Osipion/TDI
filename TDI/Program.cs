using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDI.Interception;
using TDI.Interception.Tests;

namespace TDI
{
    class Program
    {
        static void Main(string[] args)
        {
            var wrapperType = InterceptManager.Current.GetProxyType(typeof(Speaker));
            var speaker = (Speaker)Activator.CreateInstance(wrapperType);
            //InterceptManager.Current.Save();
            speaker.SayHello(1);
            speaker.SayBoo("", 1, IntPtr.Zero, 800, "B");
            try
            {
                speaker.NotSimon();
            }
            catch (InterceptException ie)
            {
                Console.WriteLine($"Whoa! Cant call 'NotSimon' with a SimonSays attribute! - Exception: {ie}");
            }
            speaker.Nothing();

            //Console.ReadLine();

            var c1 = Container.Current.Get<ConstructMe1>();
            var c2 = Container.Current.Get<ConstructMe2>();
            var s1 = Container.Current.Get<Singleton1>();
            s1.Value = "Bob";
            var s2 = Container.Current.Get<Singleton1>();

            Console.WriteLine($"This '{s2.Value}' should say 'Bob'");

            var noDefCtor = Container.Current.Get<NoEmptyCtor>();
            noDefCtor.TryToProxyMe();
            Console.WriteLine(noDefCtor.Name + "NN");
            InterceptManager.Current.Save();
            Console.ReadKey();

        }
    }
}
