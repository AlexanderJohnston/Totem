namespace Totem.Reports.Queries;

public interface IReportQueryETagFormat
{
    string Encode(ReportQueryETag etag);
    bool TryDecode(string encodedETag, [NotNullWhen(true)] out ReportQueryETag? etag);
}
