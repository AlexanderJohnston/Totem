namespace Totem.Mvc.Controllers;

public sealed class HttpMessageControllerProvider : IApplicationFeatureProvider<ControllerFeature>
{
    readonly RuntimeMap _map;

    public HttpMessageControllerProvider(RuntimeMap map) =>
        _map = map;

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach(var command in _map.Commands)
        {
            if(command.IsAssignableTo<IHttpCommand>())
            {
                var controllerType = typeof(HttpCommandController<>).MakeGenericType(command.DeclaredType);

                feature.Controllers.Add(controllerType.GetTypeInfo());
            }
        }

        foreach(var httpQuery in _map.ReportQueries.AssignableTo<IHttpReportQuery>())
        {
            var controllerType = typeof(HttpReportQueryController<,>).MakeGenericType(httpQuery.DeclaredType, httpQuery.Info.Row.DeclaredType);

            feature.Controllers.Add(controllerType.GetTypeInfo());
        }

        foreach(var httpQuery in _map.ReportListQueries.AssignableTo<IHttpReportListQuery>())
        {
            var controllerType = typeof(HttpReportListQueryController<,>).MakeGenericType(httpQuery.DeclaredType, httpQuery.Info.Row.DeclaredType);

            feature.Controllers.Add(controllerType.GetTypeInfo());
        }
    }
}
