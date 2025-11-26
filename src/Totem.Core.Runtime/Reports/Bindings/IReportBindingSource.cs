namespace Totem.Reports.Bindings;

public interface IReportBindingSource<TRow> where TRow : IReportRow
{
    Id CorrelationId { get; }
    ClaimsPrincipal Principal { get; }

    SubscriptionAddress CreateAddress();
    IReportQuery<TRow> CreateQuery();
    void NotifyChanged();
}
