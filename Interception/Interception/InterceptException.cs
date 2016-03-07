using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDI.Interception
{
    [Serializable]
    public class InterceptException : Exception
    {
        public InterceptException() : base("The action was prohibited by an interceptor.")
        {

        }

        public InterceptException(string message)
            : base(message)
        {

        }

        public InterceptException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
