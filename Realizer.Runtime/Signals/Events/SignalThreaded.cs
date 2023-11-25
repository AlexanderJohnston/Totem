using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memory.Converse;

namespace Realizer.Runtime.Signals.Events;
public class SignalThreaded : IEvent
{
    public SignalThreaded(Id memoryId, AuditorySignal signal)
    {
        MemoryId = memoryId;
        Signal = signal;
    }

    public Id MemoryId { get; }
    public AuditorySignal Signal { get; }


}
