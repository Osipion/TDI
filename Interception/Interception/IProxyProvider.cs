using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDI.Interception
{
    public interface IProxyTypeProvider
    {
        Type GetProxyType(Type t);
    }
}
