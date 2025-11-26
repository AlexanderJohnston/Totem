namespace Totem.Hosting;

public static class ReportHostingExtensions
{
    public static ITotemBuilder AddReports(this ITotemBuilder builder, Action<IReportPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<ReportBinder>()
        .AddSingleton<IReportBinder>(provider => provider.GetRequiredService<ReportBinder>())
        .AddSingleton<IReportWatcher>(provider => provider.GetRequiredService<ReportBinder>())
        .AddSingleton<ReportWhenMethodMiddleware>()
        .AddTransient<IReportPipelineBuilder, ReportPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<IReportPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        foreach(var Report in builder.Services.GetRuntimeMap().Reports)
        {
            builder.Services.AddTransient(Report.DeclaredType);
        }

        return builder;
    }

    public static IReportPipelineBuilder Use(this IReportPipelineBuilder builder, Func<IReportContext<IEvent>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new ReportMiddleware(middleware));

    public static IReportPipelineBuilder Use(this IReportPipelineBuilder builder, Func<IReportContext<IEvent>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static IReportPipelineBuilder UseWhenMethod(this IReportPipelineBuilder builder) =>
        builder.Use<ReportWhenMethodMiddleware>();
}
