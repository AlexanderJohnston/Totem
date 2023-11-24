namespace Totem.Core;

public interface ITimeline
{
    Id TimelineId { get; }
    bool HasErrors { get; }
    IEnumerable<ErrorInfo> Errors { get; }
}
