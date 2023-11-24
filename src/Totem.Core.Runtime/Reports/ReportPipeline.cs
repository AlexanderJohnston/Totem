namespace Totem.Reports;

public sealed class ReportPipeline : IReportPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<IReportMiddleware> _steps;
    readonly RuntimeMap _map;

    public ReportPipeline(ILogger<ReportPipeline> logger, IReadOnlyList<IReportMiddleware> steps, RuntimeMap map)
    {
        _logger = logger;
        _steps = steps;
        _map = map;
    }

    public async Task<IReportContext<IEvent>> RunAsync(EventEnvelope envelope, ObserverRoute route, CancellationToken cancellationToken)
    {
        var context = _map.CreateReportContext(envelope, route);
        var reportType = route.Observer;
        var reportId = route.ObserverId;
        var eventType = context.EventType;
        var eventId = context.EventId;

        _logger.LogDebug("Run report pipeline for {ReportType:l}.{ReportId:l} and event {EventType:l}.{EventId:l}", reportType, reportId, eventType, eventId);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Report pipeline cancelled for {ReportType:l}.{ReportId:l} and event {EventType:l}.{EventId:l}", reportType, reportId, eventType, eventId);

                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Report pipeline complete for {ReportType:l}.{ReportId:l} and event {EventType:l}.{EventId:l}", reportType, reportId, eventType, eventId);

                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}
