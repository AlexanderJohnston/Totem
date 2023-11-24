namespace Totem.Map.Builder;

[DebuggerDisplay("Count = {Count}")]
internal sealed class RuntimeMethodCollectionBuilder<TBuilder, TMethod> : IReadOnlyCollection<TBuilder>, IErrorCollector
    where TBuilder : RuntimeMethodBuilder<TMethod>
    where TMethod : RuntimeMethod
{
    readonly List<TBuilder> _items = new();

    public int Count => _items.Count;

    public IEnumerator<TBuilder> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Add(TBuilder item) =>
        _items.Add(item);

    internal RuntimeMethodCollection<TMethod> Build() =>
        new(from item in _items
            where item.HasValue
            select item.Build());

    public IEnumerable<RuntimeMapError> CollectErrors() =>
        _items.SelectMany(x => x.CollectErrors());
}
