using EventStore.Client;

namespace Totem.InExternal.Services;

public sealed class ReadResult
{
    public ReadResult(IReadOnlyList<ResolvedEvent> events)
    {
        Events = events;
    }

    public IReadOnlyList<ResolvedEvent> Events { get; }
}
