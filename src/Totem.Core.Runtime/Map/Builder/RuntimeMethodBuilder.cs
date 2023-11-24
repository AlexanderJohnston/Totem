namespace Totem.Map.Builder;

internal abstract class RuntimeMethodBuilder<TMethod> : RuntimeBuilder<TMethod>, IErrorCollector
    where TMethod : RuntimeMethod
{
    internal RuntimeMethodBuilder(MethodInfo info) =>
        Info = info;

    internal MethodInfo Info { get; }
    internal bool Ignored { get; set; }

    public override string ToString() =>
        $"{Info.DeclaringType}.{Info.Name}";

    public IEnumerable<RuntimeMapError> CollectErrors()
    {
        if(HasError)
        {
            yield return new RuntimeMapError(Info, Error, ErrorDetails, CollectParameterError());
        }
    }

    protected abstract RuntimeMapError? CollectParameterError();
}
