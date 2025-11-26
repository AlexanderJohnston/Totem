namespace Totem.Http;

public static class HttpMessageRoutes
{
    public static string Command(string externalType) =>
        !string.IsNullOrWhiteSpace(externalType) ? $"commands/{externalType}" : throw new ArgumentOutOfRangeException(nameof(externalType));

    public static string ReportQuery(string externalType) =>
        !string.IsNullOrWhiteSpace(externalType) ? $"queries/report/{externalType}" : throw new ArgumentOutOfRangeException(nameof(externalType));

    public static string ReportQuery(string externalType, Id id) =>
        !string.IsNullOrWhiteSpace(externalType) ? $"queries/report/{externalType}?id={id}" : throw new ArgumentOutOfRangeException(nameof(externalType));

    public static string ReportListQuery(string externalType) =>
        !string.IsNullOrWhiteSpace(externalType) ? $"queries/report/list/{externalType}" : throw new ArgumentOutOfRangeException(nameof(externalType));
}
