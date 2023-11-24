namespace Totem.Hosting;

public static class ServiceCollectionExtensions
{
    public static ITotemHttpClientBuilder AddTotemHttpClient(this IServiceCollection services, Action<TotemHttpClientOptions>? configure = null)
    {
        services
        .AddSingleton<ITotemHttpClient, TotemHttpClient>()
        .AddSingleton<IHttpRoutePrefix>(provider =>
        {
            var prefix = provider.GetRequiredService<IOptions<TotemHttpClientOptions>>().Value.RoutePrefix;

            if(string.IsNullOrWhiteSpace(prefix))
                throw new Exception($"Expected route prefix to be configured on {typeof(TotemHttpClientOptions)}");

            return new HttpRoutePrefix(prefix);
        })
        .AddHttpClient<IHttpClientAdapter, HttpClientAdapter>((provider, client) =>
        {
            client.BaseAddress = provider.GetRequiredService<IOptions<TotemHttpClientOptions>>().Value.BaseAddress;

            if(client.BaseAddress is null)
                throw new Exception($"Expected base address to be configured on {typeof(TotemHttpClientOptions)}");
        });

        if(configure is not null)
        {
            services.Configure(configure);
        }

        return new TotemHttpClientBuilder(services);
    }

    sealed class TotemHttpClientBuilder : ITotemHttpClientBuilder
    {
        internal TotemHttpClientBuilder(IServiceCollection services) =>
            Services = services;

        public IServiceCollection Services { get; }
    }
}
