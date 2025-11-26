namespace Totem.Map.Summary;

public sealed class NotificationSummary
{
    public NotificationSummary(Id typeId, Id handlerTypeId)
    {
        TypeId = typeId;
        HandlerTypeId = handlerTypeId;
    }

    public Id TypeId { get; }
    public Id HandlerTypeId { get; }
}
