namespace Totem.Mvc.Models;

internal static class ModelBindingContextExtensions
{
    internal static bool HasMethod(this ModelBindingContext bindingContext, HttpMethod method) =>
        bindingContext.HttpContext.Request.Method.Equals(method.Method, StringComparison.OrdinalIgnoreCase);
}
