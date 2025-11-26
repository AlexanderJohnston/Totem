namespace Totem.Reports.Bindings;

public interface IWebReportBindingSource<TRow> where TRow : IReportRow
{
    Id CorrelationId { get; }
    ClaimsPrincipal Principal { get; }

    IHttpReportBinding<TRow> BindHttpReport();
    SubscriptionAddress CreateAddress();
    ITspSubscription CreateSubscription();
    void NotifyChanged();
}
