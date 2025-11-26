namespace Totem.Commands;

public sealed class CommandTopicMiddleware : ICommandMiddleware
{
    readonly ITopicPipeline _topicPipeline;

    public CommandTopicMiddleware(ITopicPipeline topicPipeline) =>
        _topicPipeline = topicPipeline;

    public async Task InvokeAsync(ICommandContext<ICommand> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var route = context.CommandType.CallRoute(context);

        await _topicPipeline.RunAsync(context, route, cancellationToken);

        context.ExpectNoErrors();

        await next();
    }
}
