namespace Totem.Reports.Queries;

public sealed class ReportQueryETagFormat : IReportQueryETagFormat
{
    readonly Dictionary<string, ReportRowInfo> _rowInfosByExternalType;
    readonly IReportQueryETagEncryption _encryption;

    public ReportQueryETagFormat(RuntimeMap map, IReportQueryETagEncryption encryption)
    {
        _encryption = encryption;
        _rowInfosByExternalType = map.ReportRows.ToDictionary(x => x.Info.ExternalType, x => x.Info);
    }

    public string Encode(ReportQueryETag etag)
    {
        var scope = etag.Scope.ToString();
        var rowType = etag.RowInfo.ExternalType;
        var timelineId = etag.Checkpoint.TimelineId;
        var position = etag.Checkpoint.Position;

        return _encryption.Encrypt($"{scope}/{rowType}/{timelineId}/{position}");
    }

    public bool TryDecode(string encodedETag, [NotNullWhen(true)] out ReportQueryETag? etag)
    {
        etag = null;

        if(string.IsNullOrWhiteSpace(encodedETag))
        {
            return false;
        }

        var parts = _encryption.Decrypt(encodedETag).Split('/');

        if(parts.Length != 4)
        {
            return false;
        }

        if(parts.Length == 4
            && Enum.TryParse<ReportQueryScope>(parts[0], out var scope)
            && _rowInfosByExternalType.TryGetValue(parts[1], out var rowInfo)
            && Id.TryFrom(parts[2], out var timelineId)
            && long.TryParse(parts[3], out var position)
            && TimelinePosition.TryFrom(position, out var version))
        {
            etag = new ReportQueryETag(scope, rowInfo, new TimelineVersion(timelineId, version));
            return true;
        }

        return false;
    }
}
