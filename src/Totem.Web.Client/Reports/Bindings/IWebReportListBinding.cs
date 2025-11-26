namespace Totem.Reports.Bindings;

public interface IWebReportListBinding<TRow> : IDisposable
    where TRow : IReportRow
{
    SubscriptionAddress Address { get; }
    string? ETag { get; }
    IReportList<TRow>? Rows { get; }
    [MemberNotNullWhen(true, nameof(Rows))]
    bool HasValue { get; }
    bool HasRows { get; }
    Task LoadTask { get; }

    Task ReloadAsync(CancellationToken cancellationToken);
}
