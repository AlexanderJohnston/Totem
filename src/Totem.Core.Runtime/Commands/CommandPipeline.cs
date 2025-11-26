namespace Totem.Commands;

public sealed class CommandPipeline : ICommandPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<ICommandMiddleware> _steps;
    readonly RuntimeMap _map;

    public CommandPipeline(ILogger<CommandPipeline> logger, IReadOnlyList<ICommandMiddleware> steps, RuntimeMap map)
    {
        _logger = logger;
        _steps = steps;
        _map = map;
    }

    public async Task<ICommandContext<ICommand>> RunAsync(CommandEnvelope envelope, CancellationToken cancellationToken)
    {
        var context = _map.CreateContext(envelope);
        var commandType = context.CommandType;
        var commandId = context.CommandId;

        _logger.LogDebug("Run command pipeline for {CommandType:l}.{CommandId:l}", commandType, commandId);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Command pipeline cancelled for {CommandType:l}.{CommandId:l}", commandType, commandId);
                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Command pipeline complete for {CommandType:l}.{CommandId:l}", commandType, commandId);
                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}
