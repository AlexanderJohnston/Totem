using Totem.Queries;
using Totem.Reports;
using Totem.Reports.Queries;

namespace Totem.Mvc.Controllers;

public sealed class HttpReportQueryController<TQuery, TRow> : ControllerBase
    where TQuery : IHttpReportQuery<TRow>
    where TRow : IReportRow
{
    readonly ILogger _logger;
    readonly IReportQueryPipeline _pipeline;
    readonly IReportQueryETagFormat _etagFormat;

    public HttpReportQueryController(
        ILogger<HttpReportQueryController<TQuery, TRow>> logger,
        IReportQueryPipeline pipeline,
        IReportQueryETagFormat etagFormat)
    {
        _logger = logger;
        _pipeline = pipeline;
        _etagFormat = etagFormat;
    }

    [ErrorInfoActionFilter]
    public async Task<IActionResult> Handle([FromTotem] TQuery query, CancellationToken cancellationToken)
    {
        var envelope = new ReportQueryEnvelope(query, TryDecodeETag(), principal: User);
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

        _logger.LogWarning("Could not decode report ETag: {ETag}", etagHeader);

        return null;
    }

    async Task<IActionResult> RunPipelineAsync(ReportQueryEnvelope envelope, CancellationToken cancellationToken)
    {
        var context = await _pipeline.RunAsync(envelope, cancellationToken);
        var result = context.Result;

        context.ExpectNoErrors();

        if(result is null)
            throw new Exception("Expected pipeline to set query result or add an error");

        if(result.Version.IsStart)
        {
            return NotFound();
        }

        if(result.Row is null)
        {
            Response.Headers.ETag = Request.Headers.ETag;

            return StatusCode((int) HttpStatusCode.NotModified);
        }

        var etag = ReportQueryETag.Row(context.QueryType.Row.Info, result.Version);

        Response.Headers.ETag = _etagFormat.Encode(etag);

        return new ObjectResult(result.Row);
    }
}
