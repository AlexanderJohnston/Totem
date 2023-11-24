namespace Totem.Queries;

public sealed class HttpReportListQueryNegotiator : IHttpReportListQueryNegotiator
{
    readonly IHttpRoutePrefix _routePrefix;
    readonly TotemJsonFormat _jsonFormat;

    public HttpReportListQueryNegotiator(IHttpRoutePrefix routePrefix, TotemJsonFormat jsonFormat)
    {
        _routePrefix = routePrefix;
        _jsonFormat = jsonFormat;
    }

    public HttpRequestMessage Negotiate(IHttpReportListQueryContext<IHttpReportListQuery> context)
    {
        var route = HttpMessageRoutes.ReportListQuery(context.QueryInfo.ExternalType);
        var request = new HttpRequestMessage(HttpMethod.Get, _routePrefix.Apply(route));

        request.Headers.IfNoneMatch.ParseAdd(context.ETag);

        return request;
    }

    public void NegotiateResult(IHttpReportListQueryContext<IHttpReportListQuery> context)
    {
        if(context.Response is null)
            throw new Exception("Expected context to have a response");

        var contentType = context.Response.ContentType;
        var content = context.Response.Content;

        if(!ContentTypes.IsJson(contentType))
            throw new Exception($"Unsupported query content type: {contentType}");

        var rowsType = typeof(IReadOnlyList<>).MakeGenericType(context.QueryType);

        context.Rows = (IReadOnlyList<IReportRow>?) JsonSerializer.Deserialize(content, rowsType, _jsonFormat.Options);
    }
}
