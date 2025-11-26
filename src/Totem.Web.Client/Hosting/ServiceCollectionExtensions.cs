namespace Totem.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTotemWebClient(this IServiceCollection services) =>
        services
        .AddSingleton<WebReportBinder>()
        .AddSingleton<IWebReportBinder>(provider => provider.GetRequiredService<WebReportBinder>())
        .AddSingleton<ITspReportWatcher>(provider => provider.GetRequiredService<WebReportBinder>());
}
