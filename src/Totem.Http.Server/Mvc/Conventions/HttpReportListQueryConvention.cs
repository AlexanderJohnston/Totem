using Totem.Mvc.Controllers;
using Totem.Queries;

namespace Totem.Mvc.Conventions;

public sealed class HttpReportListQueryConvention : IControllerModelConvention
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
        if(controllerType.IsGenericType && controllerType.GetGenericTypeDefinition() == typeof(HttpReportListQueryController<,>))
        {
            var info = ReportListQueryInfo.From(controllerType.GetGenericArguments().First());

            route = HttpMessageRoutes.ReportListQuery(info.ExternalType);
            return true;
        }

        route = null;
        return false;
    }
}
