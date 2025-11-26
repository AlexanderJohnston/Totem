namespace Totem.Queries;

public sealed class HttpReportQueryNegotiator : IHttpReportQueryNegotiator
{
    delegate Id CompiledGetId(IReportQuery query);

    readonly ConcurrentDictionary<Type, CompiledGetId?> _getIdsByType = new();
    readonly IHttpRoutePrefix _routePrefix;
    readonly TotemJsonFormat _jsonFormat;

    public HttpReportQueryNegotiator(IHttpRoutePrefix routePrefix, TotemJsonFormat jsonFormat)
    {
        _routePrefix = routePrefix;
        _jsonFormat = jsonFormat;
    }

    public HttpRequestMessage Negotiate(IHttpReportQueryContext<IHttpReportQuery> context)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, GetRoute(context));

        request.Headers.IfNoneMatch.ParseAdd(context.ETag);

        return request;
    }

    public void NegotiateResult(IHttpReportQueryContext<IHttpReportQuery> context)
    {
        if(context.Response is null)
            throw new Exception("Expected context to have a response");

        var contentType = context.Response.ContentType;
        var content = context.Response.Content;

        if(!ContentTypes.IsJson(contentType))
            throw new Exception($"Unsupported query content type: {contentType}");

        context.Row = (IReportRow?) JsonSerializer.Deserialize(content, context.RowInfo.DeclaredType, _jsonFormat.Options);
    }

    string GetRoute(IHttpReportQueryContext<IHttpReportQuery> context)
    {
        var getId = _getIdsByType.GetOrAdd(context.QueryInfo.DeclaredType, TryCompileGetId);

        var route = getId is not null
            ? HttpMessageRoutes.ReportQuery(context.QueryInfo.ExternalType, getId(context.Query))
            : HttpMessageRoutes.ReportQuery(context.QueryInfo.ExternalType);

        return _routePrefix.Apply(route);
    }

    static CompiledGetId? TryCompileGetId(Type queryType)
    {
        var properties = queryType.GetProperties(BindingFlags.Instance);

        if(properties.Length == 0)
        {
            return null;
        }

        if(properties.Length > 1 || properties[0].PropertyType != typeof(Id))
            throw new Exception($"Expected no properties or a single property of type {typeof(Id)}: {queryType}");

        // query => ((TQuery) query).IdProperty

        var queryParameter = Expression.Parameter(typeof(IReportQuery), "query");
        var queryCast = Expression.Convert(queryParameter, queryType);
        var getProperty = Expression.Property(queryCast, properties[0]);
        var lambda = Expression.Lambda<CompiledGetId>(getProperty, queryParameter);

        return lambda.Compile();
    }
}
