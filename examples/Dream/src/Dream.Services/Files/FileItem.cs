namespace Dream.Files;

public sealed class FileItem
{
    public FileItem(FilePath path, long byteCount)
    {
        Path = path;
        ByteCount = byteCount >= 0 ? byteCount : throw new ArgumentOutOfRangeException(nameof(byteCount));
    }

    public FilePath Path { get; }
    public long ByteCount { get; }

    public override string ToString() =>
        $"{Path} ({ByteCount})";
}
