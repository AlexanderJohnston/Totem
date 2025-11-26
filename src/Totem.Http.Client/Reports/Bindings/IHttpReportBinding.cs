namespace Totem.Reports.Bindings;

public interface IHttpReportBinding<TRow> : IDisposable
    where TRow : IReportRow
{
    string? ETag { get; }
    TRow? Row { get; }
    [MemberNotNullWhen(true, nameof(Row))]
    bool HasValue { get; }
    Task LoadTask { get; }

    Task ReloadAsync(CancellationToken cancellationToken);
}
