namespace Totem.Map.Summary;

public sealed class EventHandlerSummary
{
    public EventHandlerSummary(Id typeId, Id eventTypeId, Id serviceTypeId)
    {
        TypeId = typeId;
        EventTypeId = eventTypeId;
        ServiceTypeId = serviceTypeId;
    }

    public Id TypeId { get; }
    public Id EventTypeId { get; }
    public Id ServiceTypeId { get; }
}
