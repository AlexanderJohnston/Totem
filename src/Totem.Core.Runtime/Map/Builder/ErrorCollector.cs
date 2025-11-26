namespace Totem.Map.Builder;

internal static class ErrorCollector
{
    internal static IEnumerable<RuntimeMapError> Collect(params IEnumerable<IErrorCollector>[] collectorLists) =>
        collectorLists.SelectMany(x => x.ConcatErrors());

    internal static IEnumerable<RuntimeMapError> ConcatErrors(this IEnumerable<IErrorCollector> collectors) =>
        collectors.SelectMany(x => x.CollectErrors());

    internal static IEnumerable<RuntimeMapError> ConcatErrors(this IErrorCollector collector, params IErrorCollector[] others) =>
        others.Prepend(collector).ConcatErrors();
}
