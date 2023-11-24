namespace Dream.Versions;

public interface IUnpackService
{
    Task<UnpackResult> UnpackAsync(Id versionId, FilePath zipPath, CancellationToken cancellationToken);
}
