namespace Totem.Events;

public sealed class EventPipeline : IEventPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<IEventMiddleware> _steps;
    readonly RuntimeMap _map;

    public EventPipeline(ILogger<EventPipeline> logger, IReadOnlyList<IEventMiddleware> steps, RuntimeMap map)
    {
        _logger = logger;
        _steps = steps;
        _map = map;
    }

    public async Task<IEventContext<IEvent>> RunAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var context = _map.CreateContext(envelope);
        var eventType = context.EventType;
        var eventId = context.EventId;

        _logger.LogDebug("Run event pipeline for {EventType:l}.{EventId:l}", eventType, eventId);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Event pipeline cancelled for {EventType:l}.{EventId:l}", eventType, eventId);
                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Event pipeline complete for {EventType:l}.{EventId:l}", eventType, eventId);
                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}
