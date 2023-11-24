namespace Totem.Hosting;

public static class ServiceCollectionExtensions
{
    public static ITotemTspClientBuilder AddTotemTspClient(
        this IServiceCollection services,
        Action<HttpConnectionOptions>? configureHttp = null,
        Action<TotemTspClientOptions>? configureTsp = null)
    {
        services
        .AddSingleton<ITotemTspClient, TotemTspClient>()
        .AddSingleton<ITspClientSerializer, TspClientSerializer>()
        .AddHostedService<TspHubConnectionService>()
        .AddSingleton<ITspHubConnection>(provider =>
        {
            var hubAddress = provider.GetRequiredService<IOptions<TotemTspClientOptions>>().Value?.HubAddress;

            if(hubAddress is null)
                throw new Exception($"Expected hub address to be configured on {typeof(TotemTspClientOptions)}");

            var connection = new HubConnectionBuilder().WithUrl(hubAddress);

            return new TspHubConnection(
                connection.Build(),
                provider.GetRequiredService<ITspClientSerializer>(),
                provider.GetRequiredService<ITspSubscriptionPipeline>(),
                provider.GetRequiredService<INotificationPipeline>());
        });

        if(configureHttp is not null)
        {
            services.Configure(configureHttp);
        }

        if(configureTsp is not null)
        {
            services.Configure(configureTsp);
        }

        return new TotemTspClientBuilder(services);
    }

    sealed class TotemTspClientBuilder : ITotemTspClientBuilder
    {
        internal TotemTspClientBuilder(IServiceCollection services) =>
            Services = services;

        public IServiceCollection Services { get; }
    }
}
