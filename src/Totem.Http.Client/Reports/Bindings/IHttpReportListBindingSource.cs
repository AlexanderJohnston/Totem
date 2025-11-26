namespace Totem.Reports.Bindings;

public interface IHttpReportListBindingSource<TRow> where TRow : IReportRow
{
    Id SubscriberId { get; }
    Id CorrelationId { get; }
    ClaimsPrincipal Principal { get; }

    IHttpReportListQuery<TRow> CreateQuery();
    void NotifyChanged();
}
