namespace Totem.Map.Summary;

public sealed class ReportSummary
{
    public ReportSummary(Id typeId, Id rowTypeId, IReadOnlyList<ObservationSummary> observations)
    {
        TypeId = typeId;
        RowTypeId = rowTypeId;
        Observations = observations;
    }

    public Id TypeId { get; }
    public Id RowTypeId { get; }
    public IReadOnlyList<ObservationSummary> Observations { get; }
}
