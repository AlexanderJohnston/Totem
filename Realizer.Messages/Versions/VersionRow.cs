namespace Realizer.Versions;

public sealed class VersionRow : ReportRow
{
    public string ZipUrl { get; set; } = "";
    public VersionZipFile ZipFile { get; set; } = null!;
    public VersionZipFolder ZipFolder { get; set; } = null!;
    
    public sealed class VersionZipFile
    {
        public string Path { get; set; } = "";
        public long ByteCount { get; set; }
    }

    public sealed class VersionZipFolder
    {
        public string Path { get; set; } = "";
        public int FileCount { get; set; }
        public long ByteCount { get; set; }
        public string ExePath { get; set; } = "";
    }
}
