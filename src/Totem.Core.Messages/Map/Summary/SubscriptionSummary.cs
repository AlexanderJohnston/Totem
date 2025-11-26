namespace Totem.Map.Summary;

public sealed class SubscriptionSummary
{
    public SubscriptionSummary(Id typeId, Id handlerTypeId)
    {
        TypeId = typeId;
        HandlerTypeId = handlerTypeId;
    }

    public Id TypeId { get; }
    public Id HandlerTypeId { get; }
}
