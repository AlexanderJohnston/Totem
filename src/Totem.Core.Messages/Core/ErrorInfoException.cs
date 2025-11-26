namespace Totem.Core;

public sealed class ErrorInfoException : Exception
{
    public ErrorInfoException(string message, IReadOnlyCollection<ErrorInfo> errors) : base(BuildMessage(message, errors)) =>
        Errors = errors;

    public ErrorInfoException(string message, Exception inner, IReadOnlyCollection<ErrorInfo> errors) : base(BuildMessage(message, errors), inner) =>
        Errors = errors;

    public ErrorInfoException(string message, params ErrorInfo[] errors) : this(message, errors as IReadOnlyCollection<ErrorInfo>)
    { }

    public ErrorInfoException(string message, Exception inner, params ErrorInfo[] errors) : this(message, inner, errors as IReadOnlyCollection<ErrorInfo>)
    { }

    public IReadOnlyCollection<ErrorInfo> Errors { get; }

    static string BuildMessage(string message, IReadOnlyCollection<ErrorInfo> errors) =>
        $"{message} ({string.Join(", ", errors)})";
}
