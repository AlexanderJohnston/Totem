namespace Totem.Mvc.Conventions;

internal static class ControllerModelExtensions
{
    internal static void ConfigureMessageRoute(this ControllerModel controller, HttpMethod method, string route)
    {
        var selector = controller.Selectors.Single();
        selector.ActionConstraints.Add(new HttpMethodActionConstraint(method));
        selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(route) { Order = 1 });
    }
}
