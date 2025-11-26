namespace Totem.Map.Summary;

public sealed class ReportListQuerySummary
{
    public ReportListQuerySummary(Id typeId, Id rowTypeId)
    {
        TypeId = typeId;
        RowTypeId = rowTypeId;
    }

    public Id TypeId { get; }
    public Id RowTypeId { get; }
}
