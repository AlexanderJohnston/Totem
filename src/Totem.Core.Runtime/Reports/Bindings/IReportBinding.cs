namespace Totem.Reports.Bindings;

public interface IReportBinding<TRow> : IDisposable
    where TRow : IReportRow
{
    SubscriptionAddress Address { get; }
    TRow? Row { get; }
    TimelineVersion? Version { get; }
    [MemberNotNullWhen(true, nameof(Row), nameof(Version))]
    bool HasValue { get; }
    Task LoadTask { get; }

    Task ReloadAsync(CancellationToken cancellationToken);
}
