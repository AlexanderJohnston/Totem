namespace Realizer.Versions;

public interface IDownloadService
{
    Task<FileItem> DownloadAsync(Uri zipUrl, CancellationToken cancellationToken);
}
