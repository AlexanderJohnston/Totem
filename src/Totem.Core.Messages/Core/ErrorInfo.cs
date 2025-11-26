namespace Totem.Core;

public sealed class ErrorInfo
{
    public ErrorInfo(string name, int code, ErrorLevel level = ErrorLevel.BadRequest)
    {
        Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentOutOfRangeException(nameof(name));
        Code = code;
        Level = level;
    }

    public ErrorInfo(string name, ErrorLevel level = ErrorLevel.BadRequest)
      : this(name, (int) level, level)
    { }

    public string Name { get; }
    public int Code { get; }
    public ErrorLevel Level { get; }

    public override string ToString() =>
        $"{Code} {Level} - {Name}";

    public ErrorInfoException ToException(string message) =>
        new(message, this);

    public ErrorInfoException ToException(string message, Exception inner) =>
        new(message, inner, this);

    public static ErrorInfo BadRequest(string name) => new(name, ErrorLevel.BadRequest);
    public static ErrorInfo Unauthorized(string name) => new(name, ErrorLevel.Unauthorized);
    public static ErrorInfo Forbidden(string name) => new(name, ErrorLevel.Forbidden);
    public static ErrorInfo NotFound(string name) => new(name, ErrorLevel.NotFound);
    public static ErrorInfo Conflict(string name) => new(name, ErrorLevel.Conflict);
    public static ErrorInfo Fatal(string name) => new(name, ErrorLevel.Fatal);
}
