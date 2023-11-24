namespace Totem.Reports.Bindings;

public interface IWebReportListBindingSource<TRow> where TRow : IReportRow
{
    Id CorrelationId { get; }
    ClaimsPrincipal Principal { get; }

    IHttpReportListBinding<TRow> BindHttpReportList();
    SubscriptionAddress CreateAddress();
    IHttpReportListQuery<TRow> CreateQuery();
    ITspSubscription CreateSubscription();
    void NotifyChanged();
}
