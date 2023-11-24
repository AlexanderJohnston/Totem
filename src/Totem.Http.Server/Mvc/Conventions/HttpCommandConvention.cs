using Totem.Commands;
using Totem.Mvc.Controllers;

namespace Totem.Mvc.Conventions;

public sealed class HttpCommandConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if(TryGetRoute(controller.ControllerType, out var route))
        {
            controller.ConfigureMessageRoute(HttpMethod.Post, route);
        }
    }

    static bool TryGetRoute(Type controllerType, [NotNullWhen(true)] out string? route)
    {
        if(controllerType.IsGenericType && controllerType.GetGenericTypeDefinition() == typeof(HttpCommandController<>))
        {
            var info = CommandInfo.From(controllerType.GetGenericArguments().Single());

            route = HttpMessageRoutes.Command(info.ExternalType);
            return true;
        }

        route = null;
        return false;
    }
}
