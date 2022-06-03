using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Totem.External
{
    public record CatchUpSubscription : EventStoreSubscription
    {
        public uint ConcurrencyLimit { get; set; } = 1;
    }
}
