using Totem.Queries;

namespace Totem.Reports;

public sealed class WatchReport : ISubscription
{
    public WatchReport(ReportQueryETag etag) =>
        ETag = etag;

    public ReportQueryETag ETag { get; }
}
