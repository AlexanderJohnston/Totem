namespace Dream.Versions;

public sealed class DownloadService : IDownloadService
{
    const string _downloadsRoot = "version-downloads";

    readonly ILogger _logger;
    readonly IHttpClientFactory _httpClientFactory;
    readonly IFileStorage _fileStorage;

    public DownloadService(ILogger<DownloadService> logger, IHttpClientFactory httpClientFactory, IFileStorage fileStorage)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _fileStorage = fileStorage;
    }

    public async Task<FileItem> DownloadAsync(Uri zipUrl, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();

        _logger.LogTrace("Download version zip at {ZipUrl:l}", zipUrl);

        using var client = _httpClientFactory.CreateClient();
        using var data = await client.GetStreamAsync(zipUrl, cancellationToken);

        var path = new FilePath(_downloadsRoot, Path.GetFileName(zipUrl.AbsolutePath));

        _logger.LogTrace("Store version zip at {ZipPath:l}", path);

        var item = await _fileStorage.PutAsync(path, data, cancellationToken);

        _logger.LogTrace("Version zip stored (bytes: {ByteCount})", item.ByteCount);

        return item;
    }
}
