namespace Dream.Versions.Topics;

public sealed class DownloadTopic : Topic
{
    public static Id Route(DownloadVersion command) => command.VersionId;

    readonly IDownloadService _service;

    public DownloadTopic(IDownloadService service) =>
        _service = service;

    public async Task When(DownloadVersion command, CancellationToken cancellationToken)
    {
        if(!Uri.TryCreate(command.ZipUrl, UriKind.Absolute, out var zipUrl))
        {
            ThenError(VersionErrors.ParseZipUrlFailed);
            return;
        }

        try
        {
            var file = await _service.DownloadAsync(zipUrl, cancellationToken);

            Then(new VersionDownloaded(command.VersionId, command.ZipUrl, file.Path.ToString(), file.ByteCount));
        }
        catch(Exception exception)
        {
            Then(new DownloadVersionFailed(command.VersionId, command.ZipUrl, exception.ToString()));
        }
    }
}
