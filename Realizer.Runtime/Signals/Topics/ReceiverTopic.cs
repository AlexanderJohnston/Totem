using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Realizer.Messages.Signals;
using Realizer.Services.Signals;

namespace Realizer.Runtime.Signals.Topics;
public class ReceiverTopic
{
    //readonly IShortTermMemory<string> _memories;
    //readonly IMultiTask _conversations;

    //public ReceiverTopic(IMultiTask service, IShortTermMemory<string> memories)
    //{
    //    _memories = memories;
    //    _conversations = service;
    //}

    //public async Task When(SignalThread command, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        var memoryId = _memories.Remember(command.Message, command.DiscriminatorValue, command.UserName, MemoryType.Unknown, command.ThreadId);
    //        var auditorySignal = Process(command, memoryId);
    //        _conversations.ListenThread(auditorySignal);

    //        Then(new SignalThreaded(Id.From(memoryId), auditorySignal));
    //    }
    //    catch(Exception exception)
    //    {
    //        //Then(new FailedToThread(command.VersionId, command.ZipUrl, exception.ToString()));
    //    }
    //}

    //private AuditorySignal Process(SignalThread signal, Guid memoryId)
    //{
    //    var auditorySignal = new AuditorySignal()
    //    {
    //        Context = signal.Context,
    //        MemoryId = memoryId,
    //        Source = signal.Source,
    //        Topic = signal.Topic,
    //        Text = signal.Message,
    //        Channel = signal.ChannelId
    //    };
    //    return auditorySignal;
    //}
}
