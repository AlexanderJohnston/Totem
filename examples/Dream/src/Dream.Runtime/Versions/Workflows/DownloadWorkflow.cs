namespace Dream.Versions.Topics;

public sealed class DownloadWorkflow : Workflow
{
    public static Id Route(VersionInstalling e) => e.VersionId;

    public void When(VersionInstalling e) =>
        ThenEnqueue(new DownloadVersion(e.VersionId, e.ZipUrl));
}
