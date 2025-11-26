using Totem.Queries;
using Totem.Reports;
using Totem.Reports.Queries;

namespace Totem.Mvc.Controllers;

public sealed class HttpReportListQueryController<TQuery, TRow> : ControllerBase
    where TQuery : IHttpReportListQuery<TRow>
    where TRow : IReportRow
{
    readonly ILogger _logger;
    readonly IReportListQueryPipeline _pipeline;
    readonly IReportQueryETagFormat _etagFormat;

    public HttpReportListQueryController(
        ILogger<HttpReportListQueryController<TQuery, TRow>> logger,
        IReportListQueryPipeline pipeline,
        IReportQueryETagFormat etagFormat)
    {
        _logger = logger;
        _pipeline = pipeline;
        _etagFormat = etagFormat;
    }

    [ErrorInfoActionFilter]
    public async Task<IActionResult> Handle([FromTotem] TQuery query, CancellationToken cancellationToken)
    {
        var envelope = new ReportListQueryEnvelope(query, TryDecodeETag(), principal: User);
        var queryType = envelope.QueryType;
        var queryId = envelope.QueryId;

        _logger.LogDebug("Run HTTP pipeline for {RequestMethod:l} {@QueryType:l}.{QueryId:l}", Request.Method, queryType, queryId);

        try
        {
            return await RunPipelineAsync(envelope, cancellationToken);
        }
        catch(ErrorInfoException exception)
        {
            _logger.LogError(exception, "HTTP pipeline failed for {RequestMethod:l} {@QueryType:l}.{QueryId:l}", Request.Method, queryType, queryId);

            return new ErrorInfoResult(exception.Errors);
        }
    }

    TimelineVersion? TryDecodeETag()
    {
        var etagHeader = Request.Headers.ETag.ToString();

        if(string.IsNullOrWhiteSpace(etagHeader))
        {
            return null;
        }

        if(_etagFormat.TryDecode(etagHeader, out var etag))
        {
            return etag.Checkpoint;
        }

        _logger.LogWarning("Could not decode report list ETag: {ETag}", etagHeader);

        return null;
    }

    async Task<IActionResult> RunPipelineAsync(ReportListQueryEnvelope envelope, CancellationToken cancellationToken)
    {
        var context = await _pipeline.RunAsync(envelope, cancellationToken);
        var result = context.Result;

        context.ExpectNoErrors();

        if(result is null)
            throw new Exception("Expected pipeline to set query result or add an error");

        if(result.Rows is null)
        {
            Response.Headers.ETag = Request.Headers.ETag;

            return StatusCode((int) HttpStatusCode.NotModified);
        }

        var etag = ReportQueryETag.List(context.QueryType.Row.Info, result.Version);

        Response.Headers.ETag = _etagFormat.Encode(etag);

        return new ObjectResult(result.Rows);
    }
}
