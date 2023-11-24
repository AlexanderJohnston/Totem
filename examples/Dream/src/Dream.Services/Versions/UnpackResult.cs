namespace Dream.Versions;

public sealed class UnpackResult
{
    public UnpackResult(long byteCount, int fileCount, FilePath exePath)
    {
        ByteCount = byteCount >= 0 ? byteCount : throw new ArgumentOutOfRangeException(nameof(byteCount));
        FileCount = fileCount >= 0 ? fileCount : throw new ArgumentOutOfRangeException(nameof(fileCount));
        ExePath = exePath;
    }

    public long ByteCount { get; }
    public int FileCount { get; }
    public FilePath ExePath { get; }
}
