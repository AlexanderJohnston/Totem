using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Totem.External
{
    public record EventPosition(ulong? Position, DateTimeOffset Created)
    {
        public EventPosition FromContext(IEventContext<Totem.IEvent> context)
            => new(context.Position, context.WhenOccurred);
    }
}
