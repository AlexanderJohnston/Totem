namespace Totem.Hosting;

public static class ServiceCollectionExtensions
{
    public static ITotemTspServerBuilder AddTotemTspServer(this IServiceCollection services)
    {
        services
        .AddSingleton<TspServerHost>()
        .AddSingleton<ITspServerBroker>(provider => provider.GetRequiredService<TspServerHost>())
        .AddSingleton<ITspWatcher>(provider => provider.GetRequiredService<TspServerHost>())
        .AddSingleton<ITspServerSerializer, TspServerSerializer>();

        return new TotemTspServerBuilder(services);
    }

    sealed class TotemTspServerBuilder : ITotemTspServerBuilder
    {
        internal TotemTspServerBuilder(IServiceCollection services) =>
            Services = services;

        public IServiceCollection Services { get; }
    }
}
