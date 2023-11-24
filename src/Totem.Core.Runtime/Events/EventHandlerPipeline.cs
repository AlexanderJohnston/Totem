namespace Totem.Events;

public sealed class EventHandlerPipeline : IEventHandlerPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<IEventHandlerMiddleware> _steps;
    readonly RuntimeMap _map;

    public EventHandlerPipeline(ILogger<EventHandlerPipeline> logger, IReadOnlyList<IEventHandlerMiddleware> steps, RuntimeMap map)
    {
        _logger = logger;
        _steps = steps;
        _map = map;
    }

    public async Task<IEventHandlerContext<IEvent>> RunAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var context = _map.CreateEventHandlerContext(envelope);
        var eventType = context.EventType;
        var eventId = context.EventId;

        _logger.LogDebug("Run event handler pipeline for {EventType:l}.{EventId:l}", eventType, eventId);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Event handler pipeline cancelled for {EventType:l}.{EventId:l}", eventType, eventId);
                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Event handler pipeline complete for {EventType:l}.{EventId:l}", eventType, eventId);
                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}
