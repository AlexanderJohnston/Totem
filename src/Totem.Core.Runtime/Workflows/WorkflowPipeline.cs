namespace Totem.Workflows;

public sealed class WorkflowPipeline : IWorkflowPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<IWorkflowMiddleware> _steps;
    readonly RuntimeMap _map;

    public WorkflowPipeline(ILogger<WorkflowPipeline> logger, IReadOnlyList<IWorkflowMiddleware> steps, RuntimeMap map)
    {
        _logger = logger;
        _steps = steps;
        _map = map;
    }

    public async Task<IWorkflowContext<IEvent>> RunAsync(EventEnvelope envelope, ObserverRoute route, CancellationToken cancellationToken)
    {
        var context = _map.CreateWorkflowContext(envelope, route);
        var workflowType = route.Observer;
        var workflowId = route.ObserverId;
        var eventType = context.EventType;
        var eventId = context.EventId;

        _logger.LogDebug("Run workflow pipeline for {WorkflowType:l}.{WorkflowId:l} and event {EventType:l}.{EventId:l}", workflowType, workflowId, eventType, eventId);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Workflow pipeline cancelled for {WorkflowType:l}.{WorkflowId:l} and event {EventType:l}.{EventId:l}", workflowType, workflowId, eventType, eventId);

                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Workflow pipeline complete for {WorkflowType:l}.{WorkflowId:l} and event {EventType:l}.{EventId:l}", workflowType, workflowId, eventType, eventId);

                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}
