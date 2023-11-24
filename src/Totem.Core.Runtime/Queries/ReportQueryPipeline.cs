namespace Totem.Queries;

public sealed class ReportQueryPipeline : IReportQueryPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<IReportQueryMiddleware> _steps;
    readonly RuntimeMap _map;

    public ReportQueryPipeline(ILogger<ReportQueryPipeline> logger, IReadOnlyList<IReportQueryMiddleware> steps, RuntimeMap map)
    {
        _logger = logger;
        _steps = steps;
        _map = map;
    }

    public async Task<IReportQueryContext<IReportQuery>> RunAsync(ReportQueryEnvelope envelope, CancellationToken cancellationToken)
    {
        var context = _map.CreateContext(envelope);
        var queryType = context.QueryType;
        var queryId = context.QueryId;

        _logger.LogDebug("Run query pipeline for {QueryType:l}.{QueryId:l}", queryType, queryId);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Query pipeline cancelled for {QueryType:l}.{QueryId:l}", queryType, queryId);
                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Query pipeline complete for {QueryType:l}.{QueryId:l}", queryType, queryId);
                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}
