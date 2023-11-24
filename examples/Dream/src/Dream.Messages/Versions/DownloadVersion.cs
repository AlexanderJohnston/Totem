namespace Dream.Versions;

public sealed class DownloadVersion : IWorkflowCommand
{
    public DownloadVersion(Id versionId, string zipUrl)
    {
        VersionId = versionId;
        ZipUrl = zipUrl;
    }

    public Id VersionId { get; }
    public string ZipUrl { get; }
}
