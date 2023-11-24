namespace Totem.Topics;

public sealed class TopicPipeline : ITopicPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<ITopicMiddleware> _steps;
    readonly RuntimeMap _map;

    public TopicPipeline(ILogger<TopicPipeline> logger, IReadOnlyList<ITopicMiddleware> steps, RuntimeMap map)
    {
        _logger = logger;
        _steps = steps;
        _map = map;
    }

    public async Task<ITopicContext<ICommand>> RunAsync(ICommandContext<ICommand> commandContext, TopicRoute route, CancellationToken cancellationToken)
    {
        var context = _map.CreateTopicContext(commandContext, route);
        var topicType = route.TopicType;
        var topicId = route.TopicId;
        var commandType = context.CommandType;
        var commandId = context.CommandId;

        _logger.LogDebug("Run topic pipeline for {TopicType:l}.{TopicId:l} and command {CommandType:l}.{CommandId:l}", topicType, topicId, commandType, commandId);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Topic pipeline cancelled for {TopicType:l}.{TopicId:l} and command {CommandType:l}.{CommandId:l}", topicType, topicId, commandType, commandId);

                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Topic pipeline complete for {TopicType:l}.{TopicId:l} and command {CommandType:l}.{CommandId:l}", topicType, topicId, commandType, commandId);

                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}
