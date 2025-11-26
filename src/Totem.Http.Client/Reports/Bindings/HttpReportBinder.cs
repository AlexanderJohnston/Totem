namespace Totem.Reports.Bindings;

public sealed class HttpReportBinder : IHttpReportBinder
{
    readonly ITotemHttpClient _httpClient;

    public HttpReportBinder(ITotemHttpClient httpClient) =>
        _httpClient = httpClient;

    public IHttpReportBinding<TRow> Bind<TRow>(IHttpReportBindingSource<TRow> source) where TRow : IReportRow =>
        new HttpReportBinding<TRow>(source, _httpClient);

    public IHttpReportListBinding<TRow> BindList<TRow>(IHttpReportListBindingSource<TRow> source) where TRow : IReportRow =>
        new HttpReportListBinding<TRow>(source, _httpClient);
}
