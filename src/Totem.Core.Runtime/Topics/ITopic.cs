namespace Totem.Topics;

public interface ITopic : ITimeline
{
    IReadOnlyCollection<IEvent> NewEvents { get; }
}
