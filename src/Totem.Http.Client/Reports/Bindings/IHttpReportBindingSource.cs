namespace Totem.Reports.Bindings;

public interface IHttpReportBindingSource<TRow> where TRow : IReportRow
{
    Id CorrelationId { get; }
    ClaimsPrincipal Principal { get; }

    IHttpReportQuery<TRow> CreateQuery();
}
