using Totem.Mvc.Controllers;
using Totem.Queries;

namespace Totem.Mvc.Conventions;

public sealed class HttpReportQueryConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if(TryGetRoute(controller.ControllerType, out var route))
        {
            controller.ConfigureMessageRoute(HttpMethod.Get, route);
        }
    }

    static bool TryGetRoute(Type controllerType, [NotNullWhen(true)] out string? route)
    {
        if(controllerType.IsGenericType && controllerType.GetGenericTypeDefinition() == typeof(HttpReportQueryController<,>))
        {
            var info = ReportQueryInfo.From(controllerType.GetGenericArguments().First());

            route = HttpMessageRoutes.ReportQuery(info.ExternalType);
            return true;
        }

        route = null;
        return false;
    }
}
