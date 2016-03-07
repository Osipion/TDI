using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using TDI.Interception;
using TDI.Interception.Tests;

namespace TDI.SampleWebHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = InterceptManager.Current.GetProxyType(typeof(MyService));
            var uri = new Uri($"http://{getIp()}:8081/MyService");

            using (var host = new WebServiceHost(service, uri))
            {
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                //smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                host.Description.Behaviors.Add(smb);

                ServiceDebugBehavior sdb = host.Description.Behaviors.Find<ServiceDebugBehavior>();

                if(sdb == null)
                {
                    sdb = new ServiceDebugBehavior();
                    host.Description.Behaviors.Add(sdb);
                }

                sdb.IncludeExceptionDetailInFaults = true;
                sdb.HttpsHelpPageEnabled = true;
                sdb.HttpHelpPageEnabled = true;

                host.Open();

                Console.WriteLine("The service is ready at {0}", uri);
                Console.WriteLine("Press <Enter> to stop the service.");
                Console.ReadLine();
            }
        }

        static string getIp()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
        }
    }

    [ServiceContract, DefaultImplementation(typeof(MyService))]
    public interface MyServiceContract
    {
        [OperationContract, WebGet]
        string SayHello();
    }

    [ServiceBehavior]
    public class MyService : MyServiceContract
    {
        [Log]
        public virtual string SayHello()
        {
            return "Hello";
        }
    }
}
