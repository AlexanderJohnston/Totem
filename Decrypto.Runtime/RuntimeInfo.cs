using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Decrypto
{
    public class RuntimeInfo
    {
        public static Assembly Assembly => typeof(RuntimeInfo).Assembly;
    }
}
