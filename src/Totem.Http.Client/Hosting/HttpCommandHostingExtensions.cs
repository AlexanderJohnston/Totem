namespace Totem.Hosting;

public static class HttpCommandHostingExtensions
{
    public static ITotemHttpClientBuilder AddHttpCommands(this ITotemHttpClientBuilder builder, Action<IHttpCommandPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<HttpCommandRequestMiddleware>()
        .AddTransient<IHttpCommandPipelineBuilder, HttpCommandPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<IHttpCommandPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        return builder;
    }

    public static IHttpCommandPipelineBuilder Use(this IHttpCommandPipelineBuilder builder, Func<IHttpCommandContext<IHttpCommand>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new HttpCommandMiddleware(middleware));

    public static IHttpCommandPipelineBuilder Use(this IHttpCommandPipelineBuilder builder, Func<IHttpCommandContext<IHttpCommand>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static IHttpCommandPipelineBuilder UseRequest(this IHttpCommandPipelineBuilder builder) =>
        builder.Use<HttpCommandRequestMiddleware>();
}
