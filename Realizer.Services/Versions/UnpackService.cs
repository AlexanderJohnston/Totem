using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Realizer.Versions;

public sealed class UnpackService : IUnpackService
{
    const string _contentRoot = "version-content";
    const string _exeName = "EventStore.ClusterNode.exe";

    readonly ILogger _logger;
    readonly IFileStorage _fileStorage;

    public UnpackService(ILogger<DownloadService> logger, IFileStorage fileStorage)
    {
        _logger = logger;
        _fileStorage = fileStorage;
    }

    public async Task<UnpackResult> UnpackAsync(Id versionId, FilePath zipPath, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Unpack version zip at {ZipPath:l}", zipPath);

        using var zipStream = await _fileStorage.GetAsync(zipPath, cancellationToken);
        using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var byteCount = 0L;
        var exePath = null as FilePath;

        foreach(var entry in zipArchive.Entries)
        {
            using var entryStream = entry.Open();

            var key = entry.FullName.Trim(FilePath.Separator);
            var file = await _fileStorage.PutAsync(_contentRoot, key, entryStream, cancellationToken);

            byteCount += file.ByteCount;

            if(Path.GetFileName(entry.FullName) == _exeName)
            {
                exePath = new FilePath(_contentRoot, key);
            }
        }

        if(exePath is null)
            throw new Exception($"Expected executable {_exeName} in zipped content");

        _logger.LogTrace("Version zip unpacked (files: {FileCount}, bytes: {ByteCount})", zipArchive.Entries.Count, byteCount);

        return new UnpackResult(byteCount, zipArchive.Entries.Count, exePath);
    }
}
