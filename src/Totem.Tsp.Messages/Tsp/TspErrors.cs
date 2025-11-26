namespace Totem.Tsp;

public static class TspErrors
{
    public static readonly ErrorInfo DecodeReportQueryETagFailed = ErrorInfo.BadRequest(nameof(DecodeReportQueryETagFailed));
}
