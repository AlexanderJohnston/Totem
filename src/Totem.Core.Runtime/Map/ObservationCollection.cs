namespace Totem.Map;

[DebuggerDisplay("Count = {Count}")]
public sealed class ObservationCollection : IReadOnlyCollection<Observation>
{
    readonly Dictionary<EventType, Observation> _observationsByEventType = new();

    public int Count => _observationsByEventType.Count;
    public Observation this[EventType eventType] => Get(eventType);
    public IEnumerable<EventType> EventTypes => _observationsByEventType.Keys;

    public IEnumerator<Observation> GetEnumerator() => _observationsByEventType.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Contains(EventType eventType) =>
        _observationsByEventType.ContainsKey(eventType);

    public bool TryGet(EventType eventType, [NotNullWhen(true)] out Observation? item) =>
        _observationsByEventType.TryGetValue(eventType, out item);

    public Observation Get(EventType eventType)
    {
        if(!TryGet(eventType, out var item))
            throw new KeyNotFoundException($"Expected collection to contain {eventType}");

        return item;
    }

    internal void Add(Observation observation) =>
        _observationsByEventType.Add(observation.Event, observation);
}
