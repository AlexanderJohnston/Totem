using EventStore.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Totem.InExternal.Services
{
    // Represents the result of an append operation
    public class AppendResult
    {
        public StreamRevision NextExpectedStreamVersion { get; set; }
        public ulong LogPosition { get; set; }
    }
}
