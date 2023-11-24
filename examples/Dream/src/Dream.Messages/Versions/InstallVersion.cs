namespace Dream.Versions;

public sealed class InstallVersion : IHttpCommand
{
    public InstallVersion(string zipUrl) =>
        ZipUrl = zipUrl;

    public string ZipUrl { get; }
}
