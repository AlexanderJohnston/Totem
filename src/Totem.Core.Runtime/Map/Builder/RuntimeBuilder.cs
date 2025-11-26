namespace Totem.Map.Builder;

internal abstract class RuntimeBuilder<TValue>
{
    bool _built;

    internal TValue Value { get; private set; } = default!;
    internal ErrorInfo? Error { get; private set; }
    internal object? ErrorDetails { get; private set; }
    [MemberNotNullWhen(false, nameof(Error))]
    internal bool HasValue => Error is null;
    [MemberNotNullWhen(true, nameof(Error))]
    internal bool HasError => Error is not null;

    internal virtual void Reflect(RuntimeMapBuilder map)
    { }

    internal virtual void Validate()
    { }

    internal TValue Build()
    {
        ExpectNoError();
        ExpectNotBuilt();

        _built = true;

        Value = BuildValue();

        return Value;
    }

    protected abstract TValue BuildValue();

    protected void SetError(ErrorInfo error, object? details = null)
    {
        ExpectNoError();

        Error = error;
        ErrorDetails = details;
    }

    void ExpectNoError()
    {
        if(HasError)
            throw new Exception($"Builder already has an error: {Error}");
    }

    void ExpectNotBuilt()
    {
        if(_built)
            throw new Exception("Value has already been built");
    }
}
