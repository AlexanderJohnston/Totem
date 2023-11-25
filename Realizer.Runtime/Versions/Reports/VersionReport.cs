namespace Realizer.Versions.Reports;

public sealed class VersionReport : Report<VersionRow>
{
    public static Id Route(VersionInstalling e) => e.VersionId;
    public static Id Route(VersionDownloaded e) => e.VersionId;
    public static Id Route(VersionUnpacked e) => e.VersionId;

    public void When(VersionInstalling e) =>
        Row.ZipUrl = e.ZipUrl;

    public void When(VersionDownloaded e) =>
        Row.ZipFile = new()
        {
            Path = e.ZipPath,
            ByteCount = e.ByteCount
        };

    public void When(VersionUnpacked e) =>
        Row.ZipFolder = new()
        {
            Path = e.ZipPath,
            FileCount = e.FileCount,
            ByteCount = e.ByteCount,
            ExePath = e.ExePath
        };
}
