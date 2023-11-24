namespace Totem.Map.Summary;

public sealed class RuntimeMapBuildErrorSummary
{
    public RuntimeMapBuildErrorSummary(
        string input,
        Id inputTypeId,
        string info,
        IReadOnlyDictionary<string, string> details,
        IReadOnlyList<RuntimeMapBuildErrorSummary> innerErrors)
    {
        Input = input;
        InputTypeId = inputTypeId;
        Info = info;
        Details = details;
        InnerErrors = innerErrors;
    }

    public string Input { get; }
    public Id InputTypeId { get; }
    public string Info { get; }
    public IReadOnlyDictionary<string, string> Details { get; }
    public IReadOnlyList<RuntimeMapBuildErrorSummary> InnerErrors { get; }
}
