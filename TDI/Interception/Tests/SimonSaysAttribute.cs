using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TDI.Interception.Tests
{
    public class SimonsSaysAttribute : InterceptAttribute
    {
        public override bool Intercept(MethodInfo info, object instance)
        {
            if (info.Name == "NotSimon") return true;

            Console.Write("Simon says: ");
            return false;
        }
    }
}
