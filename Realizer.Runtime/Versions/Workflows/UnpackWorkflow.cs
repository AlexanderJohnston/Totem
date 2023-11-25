namespace Realizer.Versions.Topics;

public sealed class UnpackWorkflow : Workflow
{
    public static Id Route(VersionDownloaded e) => e.VersionId;

    public void When(VersionDownloaded e) =>
        ThenEnqueue(new UnpackVersion(e.VersionId, e.ZipPath));
}
