using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Totem.InExternal.Services
{
    // Represents the result of a read operation
    public class ReadResult
    {
        public IEnumerable<object> Events { get; set; }
    }
}
