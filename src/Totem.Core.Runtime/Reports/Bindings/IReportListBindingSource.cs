namespace Totem.Reports.Bindings;

public interface IReportListBindingSource<TRow> where TRow : IReportRow
{
    Id SubscriberId { get; }
    Id CorrelationId { get; }
    ClaimsPrincipal Principal { get; }

    SubscriptionAddress CreateAddress();
    IReportListQuery<TRow> CreateQuery();
    void NotifyChanged();
}
