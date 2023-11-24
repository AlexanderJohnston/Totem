namespace Totem.Hosting;

public static class TopicHostingExtensions
{
    public static ITotemBuilder AddTopics(this ITotemBuilder builder, Action<ITopicPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<TopicWhenMethodMiddleware>()
        .AddTransient<ITopicPipelineBuilder, TopicPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<ITopicPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        foreach(var topic in builder.Services.GetRuntimeMap().Topics)
        {
            builder.Services.AddTransient(topic.DeclaredType);
        }

        return builder;
    }

    public static ITopicPipelineBuilder Use(this ITopicPipelineBuilder builder, Func<ITopicContext<ICommand>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new TopicMiddleware(middleware));

    public static ITopicPipelineBuilder Use(this ITopicPipelineBuilder builder, Func<ITopicContext<ICommand>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static ITopicPipelineBuilder UseWhenMethod(this ITopicPipelineBuilder builder) =>
        builder.Use<TopicWhenMethodMiddleware>();
}
