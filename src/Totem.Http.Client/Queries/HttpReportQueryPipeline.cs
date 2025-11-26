namespace Totem.Queries;

public sealed class HttpReportQueryPipeline : IHttpReportQueryPipeline
{
    readonly ILogger _logger;
    readonly IReadOnlyList<IHttpReportQueryMiddleware> _steps;
    readonly HttpReportQueryContextFactory _contextFactory;

    public HttpReportQueryPipeline(ILogger<HttpReportQueryPipeline> logger, IReadOnlyList<IHttpReportQueryMiddleware> steps)
    {
        _logger = logger;
        _steps = steps;
        _contextFactory = new();
    }

    public async Task<IHttpReportQueryContext<IHttpReportQuery>> RunAsync(HttpReportQueryEnvelope envelope, CancellationToken cancellationToken)
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
