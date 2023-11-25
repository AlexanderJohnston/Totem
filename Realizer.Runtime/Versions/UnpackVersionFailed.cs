namespace Realizer.Versions;

public sealed class UnpackVersionFailed : IEvent
{
    public UnpackVersionFailed(Id versionId, string zipPath, string error)
    {
        VersionId = versionId;
        ZipPath = zipPath;
        Error = error;
    }

    public Id VersionId { get; }
    public string ZipPath { get; }
    public string Error { get; }
}
