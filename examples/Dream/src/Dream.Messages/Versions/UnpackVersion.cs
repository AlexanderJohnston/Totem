namespace Dream.Versions;

public sealed class UnpackVersion : IWorkflowCommand
{
    public UnpackVersion(Id versionId, string zipPath)
    {
        VersionId = versionId;
        ZipPath = zipPath;
    }

    public Id VersionId { get; }
    public string ZipPath { get; }
}
