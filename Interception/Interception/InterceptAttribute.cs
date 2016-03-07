using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TDI.Interception
{
    public class InterceptAttribute : TDIAttribute, IInterceptor
    {
        public virtual bool Intercept(MethodInfo method, object instance)
        {
            return false;
        }
    }
}
