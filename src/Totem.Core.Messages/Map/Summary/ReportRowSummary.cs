namespace Totem.Map.Summary;

public sealed class ReportRowSummary
{
    public ReportRowSummary(
        Id typeId,
        Id reportTypeId,
        string externalType,
        IReadOnlyList<Id> queryTypeIds,
        IReadOnlyList<Id> listQueryTypeIds,
        IReadOnlyList<ReportRowPropertySummary> properties)
    {
        TypeId = typeId;
        ReportTypeId = reportTypeId;
        ExternalType = externalType;
        QueryTypeIds = queryTypeIds;
        ListQueryTypeIds = listQueryTypeIds;
        Properties = properties;
    }

    public Id TypeId { get; }
    public Id ReportTypeId { get; }
    public string ExternalType { get; }
    public IReadOnlyList<Id> QueryTypeIds { get; }
    public IReadOnlyList<Id> ListQueryTypeIds { get; }
    public IReadOnlyList<ReportRowPropertySummary> Properties { get; }
}
