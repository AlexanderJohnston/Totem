namespace Totem.Hosting;

public static class HttpReportQueryHostingExtensions
{
    public static ITotemHttpClientBuilder AddHttpReportQueries(this ITotemHttpClientBuilder builder, Action<IHttpReportQueryPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<HttpReportQueryRequestMiddleware>()
        .AddSingleton<IHttpReportListQueryNegotiator, HttpReportListQueryNegotiator>()
        .AddTransient<IHttpReportQueryPipelineBuilder, HttpReportQueryPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<IHttpReportQueryPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        return builder;
    }

    public static IHttpReportQueryPipelineBuilder Use(this IHttpReportQueryPipelineBuilder builder, Func<IHttpReportQueryContext<IHttpReportQuery>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new HttpReportQueryMiddleware(middleware));

    public static IHttpReportQueryPipelineBuilder Use(this IHttpReportQueryPipelineBuilder builder, Func<IHttpReportQueryContext<IHttpReportQuery>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static IHttpReportQueryPipelineBuilder UseRequest(this IHttpReportQueryPipelineBuilder builder) =>
        builder.Use<HttpReportQueryRequestMiddleware>();
}
