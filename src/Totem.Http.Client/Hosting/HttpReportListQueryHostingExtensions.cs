namespace Totem.Hosting;

public static class HttpReportListQueryHostingExtensions
{
    public static ITotemHttpClientBuilder AddHttpReportListQueries(this ITotemHttpClientBuilder builder, Action<IHttpReportListQueryPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<HttpReportListQueryRequestMiddleware>()
        .AddSingleton<IHttpReportQueryNegotiator, HttpReportQueryNegotiator>()
        .AddTransient<IHttpReportListQueryPipelineBuilder, HttpReportListQueryPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<IHttpReportListQueryPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        return builder;
    }

    public static IHttpReportListQueryPipelineBuilder Use(this IHttpReportListQueryPipelineBuilder builder, Func<IHttpReportListQueryContext<IHttpReportListQuery>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new HttpReportListQueryMiddleware(middleware));

    public static IHttpReportListQueryPipelineBuilder Use(this IHttpReportListQueryPipelineBuilder builder, Func<IHttpReportListQueryContext<IHttpReportListQuery>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static IHttpReportListQueryPipelineBuilder UseRequest(this IHttpReportListQueryPipelineBuilder builder) =>
        builder.Use<HttpReportListQueryRequestMiddleware>();
}
