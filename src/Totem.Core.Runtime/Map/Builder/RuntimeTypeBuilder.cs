namespace Totem.Map.Builder;

internal abstract class RuntimeTypeBuilder<TType> : RuntimeBuilder<TType>, IErrorCollector
    where TType : RuntimeType
{
    protected RuntimeTypeBuilder(Type declaredType) =>
        DeclaredType = declaredType;

    internal Type DeclaredType { get; }

    public override string ToString() =>
        DeclaredType.Name;

    public IEnumerable<RuntimeMapError> CollectErrors()
    {
        if(HasError)
        {
            yield return new RuntimeMapError(DeclaredType, Error, ErrorDetails, CollectMethodErrors().ToList());
        }
    }

    protected virtual IEnumerable<RuntimeMapError> CollectMethodErrors() =>
        Enumerable.Empty<RuntimeMapError>();
}
