namespace Totem.Hosting;

public static class CommandHostingExtensions
{
    public static ITotemBuilder AddCommands(this ITotemBuilder builder, Action<ICommandPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<CommandTopicMiddleware>()
        .AddTransient<ICommandPipelineBuilder, CommandPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<ICommandPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        foreach(var topic in builder.Services.GetRuntimeMap().Topics)
        {
            builder.Services.AddTransient(topic.DeclaredType);
        }

        return builder;
    }

    public static ICommandPipelineBuilder Use(this ICommandPipelineBuilder builder, Func<ICommandContext<ICommand>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new CommandMiddleware(middleware));

    public static ICommandPipelineBuilder Use(this ICommandPipelineBuilder builder, Func<ICommandContext<ICommand>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static ICommandPipelineBuilder UseTopic(this ICommandPipelineBuilder builder) =>
        builder.Use<CommandTopicMiddleware>();
}
