namespace Totem;

public abstract class Topic : Timeline, ITopic
{
    readonly ConcurrentQueue<IEvent> _newEvents = new();

    public bool HasNewEvents =>
        !_newEvents.IsEmpty;

    public IReadOnlyCollection<IEvent> NewEvents => _newEvents;

    protected void Then(IEvent e)
    {
        if(HasErrors)
            throw new Exception($"Topic {this} has one or more errors preventing {e} from occurring");

        _newEvents.Enqueue(e);
    }

    protected void Then(IEnumerable<IEvent> events)
    {
        foreach(var e in events)
        {
            Then(e);
        }
    }

    protected void Then(params IEvent[] events) =>
        Then(events.AsEnumerable());
}
