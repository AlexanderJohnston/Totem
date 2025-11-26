namespace Totem.Queries;

public sealed class HttpReportListQueryPipeline : IHttpReportListQueryPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<IHttpReportListQueryMiddleware> _steps;
    readonly HttpReportListQueryContextFactory _contextFactory;

    public HttpReportListQueryPipeline(ILogger<HttpReportListQueryPipeline> logger, IReadOnlyList<IHttpReportListQueryMiddleware> steps)
    {
        _logger = logger;
        _steps = steps;
        _contextFactory = new();
    }

    public async Task<IHttpReportListQueryContext<IHttpReportListQuery>> RunAsync(HttpReportListQueryEnvelope envelope, CancellationToken cancellationToken)
    {
        var queryType = envelope.QueryType;
        var queryId = envelope.QueryId;

        _logger.LogDebug("Run query pipeline for {@QueryType:l}.{QueryId:l}", queryType, queryId);

        var context = _contextFactory.Create(envelope);

        await RunStepAsync(0);

        return context;

        async Task RunStepAsync(int index)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Query pipeline cancelled for {@QueryType:l}.{QueryId:l}", queryType, queryId);
                return;
            }

            if(index >= _steps.Count)
            {
                _logger.LogTrace("Query pipeline complete for {@QueryType:l}.{QueryId:l}", queryType, queryId);
                return;
            }

            await _steps[index].InvokeAsync(context!, () => RunStepAsync(index + 1), cancellationToken);
        }
    }
}
