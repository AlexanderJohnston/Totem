namespace Totem.Commands;

public sealed class HttpCommandPipeline : IHttpCommandPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<IHttpCommandMiddleware> _steps;
    readonly HttpCommandContextFactory _contextFactory;

    public HttpCommandPipeline(ILogger<HttpCommandPipeline> logger, IReadOnlyList<IHttpCommandMiddleware> steps)
    {
        _logger = logger;
        _steps = steps;
        _contextFactory = new();
    }

    public async Task<IHttpCommandContext<IHttpCommand>> RunAsync(HttpCommandEnvelope envelope, CancellationToken cancellationToken)
    {
        var commandType = envelope.CommandType;
        var commandId = envelope.CommandId;

        _logger.LogDebug("Run command pipeline for {@CommandType:l}.{CommandId:l}", commandType, commandId);

        var context = _contextFactory.Create(envelope);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Command pipeline cancelled for {@CommandType:l}.{CommandId:l}", commandType, commandId);
                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Command pipeline complete for {@CommandType:l}.{CommandId:l}", commandType, commandId);
                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}
