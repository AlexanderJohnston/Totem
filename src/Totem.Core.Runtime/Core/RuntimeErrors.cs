namespace Totem.Core;

public static class RuntimeErrors
{
    public static readonly ErrorInfo ReportQueryResultNotSet = ErrorInfo.Fatal(nameof(ReportQueryResultNotSet));
    public static readonly ErrorInfo ReportRowTypeNotFound = ErrorInfo.NotFound(nameof(ReportRowTypeNotFound));
}
