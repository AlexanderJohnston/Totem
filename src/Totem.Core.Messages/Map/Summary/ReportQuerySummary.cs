namespace Totem.Map.Summary;

public sealed class ReportQuerySummary
{
    public ReportQuerySummary(Id typeId, Id rowTypeId, string? idProperty)
    {
        TypeId = typeId;
        RowTypeId = rowTypeId;
        IdProperty = idProperty;
    }

    public Id TypeId { get; }
    public Id RowTypeId { get; }
    public string? IdProperty { get; }
}
