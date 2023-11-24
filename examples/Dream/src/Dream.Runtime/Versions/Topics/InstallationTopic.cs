namespace Dream.Versions.Topics;

public sealed class InstallationTopic : Topic
{
    readonly HashSet<string> _zipUrls = new();
    bool _installing;

    // Given

    public void Given(VersionInstalling e)
    {
        _zipUrls.Add(e.ZipUrl);
        _installing = true;
    }

    // When

    public void When(InstallVersion command)
    {
        if(_installing)
        {
            ThenError(VersionErrors.AlreadyInstalling);
            return;
        }

        if(!Uri.TryCreate(command.ZipUrl, UriKind.Absolute, out _))
        {
            ThenError(VersionErrors.ParseZipUrlFailed);
            return;
        }

        if(_zipUrls.Contains(command.ZipUrl))
        {
            ThenError(VersionErrors.AlreadyInstalled);
            return;
        }

        var versionId = TimelineId.DeriveId(command.ZipUrl);

        Then(new VersionInstalling(versionId, command.ZipUrl));
    }
}
