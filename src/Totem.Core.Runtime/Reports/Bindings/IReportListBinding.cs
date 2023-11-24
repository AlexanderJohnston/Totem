namespace Totem.Reports.Bindings;

public interface IReportListBinding<TRow> : IDisposable
    where TRow : IReportRow
{
    SubscriptionAddress Address { get; }
    IReportList<TRow>? Rows { get; }
    TimelineVersion? Version { get; }
    [MemberNotNullWhen(true, nameof(Rows), nameof(Version))]
    bool HasValue { get; }
    bool HasRows { get; }
    Task LoadTask { get; }

    Task ReloadAsync(CancellationToken cancellationToken);
}
