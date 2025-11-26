namespace Totem.Hosting;

public static class TotemTspClientBuilderExtensions
{
    public static ITotemTspClientBuilder ConfigureWebAssembly(
        this ITotemTspClientBuilder builder,
        string baseAddress,
        string hubRoute = "_totem/tsp")
    {
        builder.Services.Configure<HttpConnectionOptions>(options =>
        {
            var originalFactory = options.HttpMessageHandlerFactory;

            options.HttpMessageHandlerFactory = inner =>
            {
                var handler = new SameOriginCredentialsHandler { InnerHandler = inner };

                return originalFactory is null ? handler : originalFactory(handler);
            };
        });

        builder.Services.Configure<TotemTspClientOptions>(options =>
        {
            options.HubAddress = new Uri(new Uri(baseAddress), hubRoute);
        });

        return builder;
    }
}
