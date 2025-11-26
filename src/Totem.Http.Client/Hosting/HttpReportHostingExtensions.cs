namespace Totem.Hosting;

public static class HttpReportHostingExtensions
{
    public static ITotemHttpClientBuilder AddHttpReportBindings(this ITotemHttpClientBuilder builder)
    {
        builder.Services.AddSingleton<IHttpReportBinder, HttpReportBinder>();

        return builder;
    }
}
