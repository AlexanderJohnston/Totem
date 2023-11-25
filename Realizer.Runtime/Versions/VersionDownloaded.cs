namespace Realizer.Versions;

public sealed class VersionDownloaded : IEvent
{
    public VersionDownloaded(Id versionId, string zipUrl, string zipPath, long byteCount)
    {
        VersionId = versionId;
        ZipUrl = zipUrl;
        ZipPath = zipPath;
        ByteCount = byteCount >= 0 ? byteCount : throw new ArgumentOutOfRangeException(nameof(byteCount));
    }

    public Id VersionId { get; }
    public string ZipUrl { get; }
    public string ZipPath { get; }
    public long ByteCount { get; }
}
