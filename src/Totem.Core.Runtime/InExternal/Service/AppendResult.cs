using EventStore.Client;

namespace Totem.InExternal.Services;

public sealed class AppendResult
{
    public AppendResult(StreamRevision nextExpectedStreamVersion, ulong logPosition)
    {
        NextExpectedStreamVersion = nextExpectedStreamVersion;
        LogPosition = logPosition;
    }

    public StreamRevision NextExpectedStreamVersion { get; }
    public ulong LogPosition { get; }
}
