namespace Realizer.Versions;

public sealed class DownloadVersionFailed : IEvent
{
    public DownloadVersionFailed(Id versionId, string zipUrl, string error)
    {
        VersionId = versionId;
        ZipUrl = zipUrl;
        Error = error;
    }

    public Id VersionId { get; }
    public string ZipUrl { get; }
    public string Error { get; }
}
