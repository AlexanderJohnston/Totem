namespace Totem.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTotemHttpServer(this IServiceCollection services, string internalPrefix = "_totem") =>
        services.AddSingleton<IConfigureOptions<MvcOptions>>(provider =>
            new ConfigureTotemMvcOptions(internalPrefix, provider.GetRequiredService<RuntimeMap>()));
}
