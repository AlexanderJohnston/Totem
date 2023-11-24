namespace Dream.Versions;

public sealed class VersionInstalling : IEvent
{
    public VersionInstalling(Id versionId, string zipUrl)
    {
        VersionId = versionId;
        ZipUrl = zipUrl;
    }

    public Id VersionId { get; }
    public string ZipUrl { get; }
}
