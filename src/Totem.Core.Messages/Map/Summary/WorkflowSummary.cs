namespace Totem.Map.Summary;

public sealed class WorkflowSummary
{
    public WorkflowSummary(Id typeId, IReadOnlyList<ObservationSummary> observations)
    {
        TypeId = typeId;
        Observations = observations;
    }

    public Id TypeId { get; }
    public IReadOnlyList<ObservationSummary> Observations { get; }
}
