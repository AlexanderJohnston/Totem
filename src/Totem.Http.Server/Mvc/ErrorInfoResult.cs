namespace Totem.Mvc;

public sealed class ErrorInfoResult : ObjectResult
{
    public ErrorInfoResult(IEnumerable<ErrorInfo> errors) : base(errors)
    {
        errors = errors as ErrorInfo[] ?? errors.ToArray();

        Value = errors;
        StatusCode = errors
            .Select(error => (int) error.Level)
            .DefaultIfEmpty((int) HttpStatusCode.InternalServerError)
            .Max();
    }
}
