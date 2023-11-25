using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Memory.Converse;
using Realization.Skill;

namespace Realizer.Services.Signals;
public interface IMultiTask
{
    public VentralStream? GetThread(ulong id);

    public VentralStream? GetChannel(ulong id);

    // This method is used to listen to a signal and then categorize it by its thread.
    // It allows for multiple ventral streams to be used at once based on id.
    public void ListenThread(AuditorySignal signal);

}
