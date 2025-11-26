namespace Totem.Map.Summary;

public sealed class EventSummary
{
    public EventSummary(Id typeId, EventHandlerSummary? handler, IReadOnlyList<Id> reportTypeIds, IReadOnlyList<Id> workflowTypeIds)
    {
        TypeId = typeId;
        Handler = handler;
        ReportTypeIds = reportTypeIds;
        WorkflowTypeIds = workflowTypeIds;
    }

    public Id TypeId { get; }
    public EventHandlerSummary? Handler { get; }
    public IReadOnlyList<Id> ReportTypeIds { get; }
    public IReadOnlyList<Id> WorkflowTypeIds { get; }
}
