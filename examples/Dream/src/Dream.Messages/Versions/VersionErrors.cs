namespace Dream.Versions;

public static class VersionErrors
{
    public static readonly ErrorInfo AlreadyInstalled = ErrorInfo.Conflict(nameof(AlreadyInstalled));
    public static readonly ErrorInfo AlreadyInstalling = ErrorInfo.Conflict(nameof(AlreadyInstalling));
    public static readonly ErrorInfo ParseZipPathFailed = ErrorInfo.Conflict(nameof(ParseZipPathFailed));
    public static readonly ErrorInfo ParseZipUrlFailed = ErrorInfo.Conflict(nameof(ParseZipUrlFailed));
}
