namespace Totem.Map.Builder;

internal sealed class RuntimeTypeCollectionBuilder<TBuilder, TType> : RuntimeBuilder<RuntimeTypeCollection<TType>>, IReadOnlyCollection<TBuilder>, IErrorCollector
    where TBuilder : RuntimeTypeBuilder<TType>
    where TType : RuntimeType
{
    readonly Dictionary<Type, TBuilder> _itemsByDeclaredType = new();

    public int Count => _itemsByDeclaredType.Count;

    public IEnumerator<TBuilder> GetEnumerator() => _itemsByDeclaredType.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Add(TBuilder item) =>
        _itemsByDeclaredType.Add(item.DeclaredType, item);

    internal bool TryGet(Type declaredType, [NotNullWhen(true)] out TBuilder? item) =>
        _itemsByDeclaredType.TryGetValue(declaredType, out item);

    internal override void Reflect(RuntimeMapBuilder map)
    {
        foreach(var item in _itemsByDeclaredType.Values)
        {
            item.Reflect(map);
        }
    }

    internal override void Validate()
    {
        foreach(var item in _itemsByDeclaredType.Values)
        {
            if(item.HasValue)
            {
                item.Validate();
            }
        }
    }

    protected override RuntimeTypeCollection<TType> BuildValue() =>
        new(from item in _itemsByDeclaredType.Values
            where item.HasValue
            select item.Build());

    public IEnumerable<RuntimeMapError> CollectErrors() =>
        from item in _itemsByDeclaredType.Values
        where item.HasError
        from error in item.CollectErrors()
        select error;
}
