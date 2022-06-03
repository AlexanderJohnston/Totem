using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;

namespace Totem.External
{
    public record AllSusbcription : CatchUpSubscription
    {
        public IEventFilter? EventFilter { get; set; }
        public uint CheckpointInterval { get; set; } = 10;
    }
}
