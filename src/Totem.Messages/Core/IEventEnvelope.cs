using System;

namespace Totem.Core
{
    public interface IEventEnvelope : IMessageEnvelope
    {
        new IEvent Message { get; }
        Type TimelineType { get; }
        Id TimelineId { get; }
        long TimelineVersion { get; }
        DateTimeOffset WhenOccurred { get; }
    }
}