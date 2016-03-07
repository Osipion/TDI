using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDI
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class DefaultImplementationAttribute : TDIAttribute
    {
        public Type DefaultImplementationType { get; set; }

        public DefaultImplementationAttribute(Type type)
        {
            this.DefaultImplementationType = type;
        }
    }
}
