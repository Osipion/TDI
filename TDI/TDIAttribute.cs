using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDI
{
    public class TDIAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class InjectableAssemblyAttribute : Attribute
    {
        public bool Injectable { get; }

        public InjectableAssemblyAttribute(bool injectable)
        {
            this.Injectable = injectable;
        }
    }
}
