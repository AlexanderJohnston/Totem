namespace Totem.Hosting;

public static class QueryHostingExtensions
{
    public static ITotemBuilder AddReportQueries(this ITotemBuilder builder, Action<IReportQueryPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<ReportQueryReaderMiddleware>()
        .AddTransient<IReportQueryPipelineBuilder, ReportQueryPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<IReportQueryPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        builder.Services.TryAddSingleton<IReportQueryETagFormat, ReportQueryETagFormat>();
        builder.Services.TryAddSingleton<IReportQueryETagEncryption, ReportQueryETagEncryption>();

        return builder;
    }

    public static IReportQueryPipelineBuilder Use(this IReportQueryPipelineBuilder builder, Func<IReportQueryContext<IReportQuery>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new ReportQueryMiddleware(middleware));

    public static IReportQueryPipelineBuilder Use(this IReportQueryPipelineBuilder builder, Func<IReportQueryContext<IReportQuery>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static IReportQueryPipelineBuilder UseReader(this IReportQueryPipelineBuilder builder) =>
        builder.Use<ReportQueryReaderMiddleware>();

    public static ITotemBuilder AddReportListQueries(this ITotemBuilder builder, Action<IReportListQueryPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<ReportListQueryReaderMiddleware>()
        .AddTransient<IReportListQueryPipelineBuilder, ReportListQueryPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<IReportListQueryPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        builder.Services.TryAddSingleton<IReportQueryETagFormat, ReportQueryETagFormat>();
        builder.Services.TryAddSingleton<IReportQueryETagEncryption, ReportQueryETagEncryption>();

        return builder;
    }

    public static IReportListQueryPipelineBuilder Use(this IReportListQueryPipelineBuilder builder, Func<IReportListQueryContext<IReportListQuery>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new ReportListQueryMiddleware(middleware));

    public static IReportListQueryPipelineBuilder Use(this IReportListQueryPipelineBuilder builder, Func<IReportListQueryContext<IReportListQuery>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static IReportListQueryPipelineBuilder UseReader(this IReportListQueryPipelineBuilder builder) =>
        builder.Use<ReportListQueryReaderMiddleware>();
}
