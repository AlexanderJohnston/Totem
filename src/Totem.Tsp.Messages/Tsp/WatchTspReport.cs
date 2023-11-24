namespace Totem.Tsp;

public sealed class WatchTspReport : ITspSubscription
{
    public WatchTspReport(string etag) =>
        ETag = !string.IsNullOrWhiteSpace(etag) ? etag : throw new ArgumentOutOfRangeException(nameof(etag));

    public string ETag { get; }
}
