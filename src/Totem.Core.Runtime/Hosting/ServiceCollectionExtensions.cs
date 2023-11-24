using Totem.Map.Builder;

namespace Totem.Hosting;

public static class ServiceCollectionExtensions
{
    public static ITotemBuilder AddTotemRuntime(this IServiceCollection services, IEnumerable<Type> mapTypes)
    {
        var map = new RuntimeMapBuilder(mapTypes).Build();

        services
        .AddSingleton(map)
        .AddSingleton<IClock, UtcClock>()
        .AddSingleton<TotemJsonFormat>();

        services.AddHostedService<RuntimeMapErrorLoggingService>();

        return new TotemBuilder(services);
    }

    public static ITotemBuilder AddTotemRuntime(this IServiceCollection services, IEnumerable<Assembly> mapAssemblies) =>
        services.AddTotemRuntime(mapAssemblies.SelectMany(x => x.GetExportedTypes()));

    public static ITotemBuilder AddTotemRuntime(this IServiceCollection services, params Type[] mapTypes) =>
        services.AddTotemRuntime(mapTypes.AsEnumerable());

    public static ITotemBuilder AddTotemRuntime(this IServiceCollection services, params Assembly[] mapAssemblies) =>
        services.AddTotemRuntime(mapAssemblies.AsEnumerable());

    public static RuntimeMap GetRuntimeMap(this IServiceCollection services)
    {
        var map = (RuntimeMap?) services
            .LastOrDefault(x => x.ServiceType == typeof(RuntimeMap))
            ?.ImplementationInstance;

        return map ?? throw new Exception($"Expected runtime map to be built. Call {nameof(AddTotemRuntime)} first.");
    }

    sealed class TotemBuilder : ITotemBuilder
    {
        internal TotemBuilder(IServiceCollection services) =>
            Services = services;

        public IServiceCollection Services { get; }
    }
}
