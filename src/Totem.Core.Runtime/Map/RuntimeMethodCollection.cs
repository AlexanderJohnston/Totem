namespace Totem.Map;

[DebuggerDisplay("Count = {Count}")]
public sealed class RuntimeMethodCollection<T> : IReadOnlyCollection<T>
    where T : RuntimeMethod
{
    readonly Dictionary<MessageType, T> _itemsByMessageType;

    internal RuntimeMethodCollection(IEnumerable<T> items) =>
        _itemsByMessageType = items.ToDictionary(item => item.Parameter.Message);

    public int Count => _itemsByMessageType.Count;
    public T this[MessageType messageType] => Get(messageType);
    public IEnumerable<MessageType> MessageTypes => _itemsByMessageType.Keys;

    public IEnumerator<T> GetEnumerator() => _itemsByMessageType.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Contains(MessageType messageType) =>
        _itemsByMessageType.ContainsKey(messageType);

    public bool TryGet(MessageType messageType, [NotNullWhen(true)] out T? item) =>
        _itemsByMessageType.TryGetValue(messageType, out item);

    public T Get(MessageType messageType)
    {
        if(!TryGet(messageType, out var item))
            throw new KeyNotFoundException($"Expected collection to contain {messageType}");

        return item;
    }
}
