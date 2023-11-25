namespace Realizer.Versions.Topics;

public sealed class UnpackTopic : Topic
{
    public static Id Route(UnpackVersion command) => command.VersionId;

    readonly IUnpackService _service;

    public UnpackTopic(IUnpackService service) =>
        _service = service;

    public async Task When(UnpackVersion command, CancellationToken cancellationToken)
    {
        if(!FilePath.TryFrom(command.ZipPath, out var zipPath))
        {
            ThenError(VersionErrors.ParseZipPathFailed);
            return;
        }

        try
        {
            var result = await _service.UnpackAsync(command.VersionId, zipPath, cancellationToken);

            Then(new VersionUnpacked(
                command.VersionId,
                command.ZipPath,
                result.FileCount,
                result.ByteCount,
                result.ExePath.ToString()));
        }
        catch(Exception exception)
        {
            Then(new UnpackVersionFailed(command.VersionId, command.ZipPath, exception.ToString()));
        }
    }
}
