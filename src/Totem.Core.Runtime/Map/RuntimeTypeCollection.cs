namespace Totem.Map;

[DebuggerDisplay("Count = {Count}")]
public sealed class RuntimeTypeCollection<T> : IReadOnlyCollection<T>
    where T : RuntimeType
{
    readonly Dictionary<Type, T> _itemsByDeclaredType;

    internal RuntimeTypeCollection() =>
        _itemsByDeclaredType = new();

    internal RuntimeTypeCollection(IEnumerable<T> items) =>
        _itemsByDeclaredType = items.ToDictionary(item => item.DeclaredType);

    public int Count => _itemsByDeclaredType.Count;
    public T this[Type declaredType] => Get(declaredType);
    public IEnumerable<Type> DeclaredTypes => _itemsByDeclaredType.Keys;

    public IEnumerator<T> GetEnumerator() => _itemsByDeclaredType.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Contains(Type declaredType) =>
        _itemsByDeclaredType.ContainsKey(declaredType);

    public bool TryGet(Type declaredType, [NotNullWhen(true)] out T? item) =>
        _itemsByDeclaredType.TryGetValue(declaredType, out item);

    public T Get(Type declaredType)
    {
        if(!TryGet(declaredType, out var item))
            throw new KeyNotFoundException($"Expected collection to contain {declaredType}");

        return item;
    }

    public IEnumerable<T> AssignableTo(Type itemType) =>
        this.Where(type => type.IsAssignableTo(itemType));

    public IEnumerable<T> AssignableTo<TMessage>() where TMessage : IMessage =>
        this.Where(item => item.IsAssignableTo<TMessage>());

    internal void Add(T item) =>
        _itemsByDeclaredType.Add(item.DeclaredType, item);
}
