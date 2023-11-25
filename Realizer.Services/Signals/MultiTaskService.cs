using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Memory.Converse;
using Realization.Skill;

namespace Realizer.Services.Signals;
public class MultiTaskService : IMultiTask
{
    readonly ILogger _logger;
    public Dictionary<ulong, VentralStream> Channels = new();
    public Dictionary<ulong, VentralStream> Threads = new();

    public VentralStream? GetChannel(ulong id) => throw new NotImplementedException();
    public VentralStream? GetThread(ulong id) => throw new NotImplementedException();
    public void ListenThread(AuditorySignal signal)
    {
        _logger.LogTrace($"Signal in thread {0} received from source {1} with memory ID of {2}.",
            signal.Thread, signal.Source, signal.MemoryId);

        if(signal.Thread != default)
        {
            // Check if the signal matches a thread.
            if(Threads.ContainsKey(signal.Thread))
            {
                Threads[signal.Channel].Listen(signal);
                return;
            }
            else
            {
                var ventralStream = new VentralStream();
                ventralStream.Listen(signal);
                Threads.Add(signal.Thread, ventralStream);
                return;
            }
        }
    }
}
