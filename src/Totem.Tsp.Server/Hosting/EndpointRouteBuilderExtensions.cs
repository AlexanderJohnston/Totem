namespace Totem.Hosting;

public static class EndpointRouteBuilderExtensions
{
    public static HubEndpointConventionBuilder MapTspHub(this IEndpointRouteBuilder endpoints, string pattern = "_totem/tsp") =>
        endpoints.MapHub<TspHub>(pattern);
}
