namespace Dream.Versions;

public sealed class GetVersion : IHttpReportQuery<VersionRow>
{
    public GetVersion(Id versionId) =>
        VersionId = versionId;

    public Id VersionId { get; }
}
