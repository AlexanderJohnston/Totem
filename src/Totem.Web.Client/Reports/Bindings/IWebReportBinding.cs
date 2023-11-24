namespace Totem.Reports.Bindings;

public interface IWebReportBinding<TRow> : IDisposable
    where TRow : IReportRow
{
    SubscriptionAddress Address { get; }
    string? ETag { get; }
    TRow? Row { get; }
    [MemberNotNullWhen(true, nameof(Row))]
    bool HasValue { get; }
    Task LoadTask { get; }

    Task ReloadAsync(CancellationToken cancellationToken);
}
