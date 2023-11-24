namespace Dream.Versions;

public sealed class VersionUnpacked : IEvent
{
    public VersionUnpacked(Id versionId, string zipPath, int fileCount, long byteCount, string exePath)
    {
        VersionId = versionId;
        ZipPath = zipPath;
        FileCount = fileCount >= 0 ? fileCount : throw new ArgumentOutOfRangeException(nameof(fileCount));
        ByteCount = byteCount >= 0 ? byteCount : throw new ArgumentOutOfRangeException(nameof(byteCount));
        ExePath = exePath;
    }

    public Id VersionId { get; }
    public string ZipPath { get; }
    public int FileCount { get; }
    public long ByteCount { get; }
    public string ExePath { get; }
}
