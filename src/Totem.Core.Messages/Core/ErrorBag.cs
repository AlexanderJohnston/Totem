namespace Totem.Core;

public sealed class ErrorBag : ConcurrentBag<ErrorInfo>
{
    public ErrorBag()
    { }

    public ErrorBag(IEnumerable<ErrorInfo> errors) : base(errors)
    { }

    public ErrorBag(params ErrorInfo[] errors) : base(errors)
    { }

    public bool Any => !IsEmpty;

    public override string ToString() =>
        string.Join(Environment.NewLine, from error in this select error.ToString());

    public ErrorInfoException ToException(string message) =>
        new(message, ToArray());

    public ErrorInfoException ToException(string message, Exception inner) =>
        new(message, inner, ToArray());

    public void CopyTo(ErrorBag other)
    {
        foreach(var error in this)
        {
            other.Add(error);
        }
    }

    public void ExpectNone()
    {
        if(Count > 0)
            throw ToException($"Expected no errors but found {Count}");
    }
}
