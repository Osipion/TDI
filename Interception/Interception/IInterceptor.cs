using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TDI.Interception
{
    public interface IInterceptor
    {
        /// <summary>
        /// A method called by the <see cref="InterceptManager"/> when an appropriate method call is intercepted
        /// </summary>
        /// <param name="method">The method that has been intercepted</param>
        /// <param name="instance">The instance on which the method was called</param>
        /// <returns>True to halt execution and return default, false to allow the intercepted method to continue executing</returns>
        bool Intercept(MethodInfo method, object instance);
    }
}
