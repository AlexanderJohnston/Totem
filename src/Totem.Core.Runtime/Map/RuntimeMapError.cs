namespace Totem.Map;

public sealed class RuntimeMapError
{
    public RuntimeMapError(object input, ErrorInfo info, object? details, IEnumerable<RuntimeMapError> innerErrors)
    {
        Input = input;
        Info = info;
        Details = details;
        InnerErrors = innerErrors.ToArray();
    }

    public RuntimeMapError(object input, ErrorInfo info, object? details, RuntimeMapError? innerError = null)
    {
        Input = input;
        Info = info;
        Details = details;
        InnerErrors = innerError is null ? Array.Empty<RuntimeMapError>() : new[] { innerError };
    }

    public object Input { get; }
    public Type InputType => Input.GetType();
    public ErrorInfo Info { get; }
    public object? Details { get; }
    public IReadOnlyList<RuntimeMapError> InnerErrors { get; }

    public override string ToString() =>
        $"{Input}: {Info}";
}
