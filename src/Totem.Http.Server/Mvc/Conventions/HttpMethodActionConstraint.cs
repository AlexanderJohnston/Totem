namespace Totem.Mvc.Conventions;

public sealed class HttpMethodActionConstraint : IActionConstraint, IActionConstraintMetadata
{
    readonly HttpMethod _method;

    public HttpMethodActionConstraint(HttpMethod method) =>
        _method = method;

    public int Order => 100;

    public bool Accept(ActionConstraintContext context) =>
        context.RouteContext.HttpContext.Request.Method.Equals(_method.Method, StringComparison.OrdinalIgnoreCase);
}
