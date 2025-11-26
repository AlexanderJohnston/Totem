namespace Totem.Map.Summary;

public sealed class SubscriptionHandlerSummary
{
    public SubscriptionHandlerSummary(Id typeId, Id subscriptionTypeId, Id serviceTypeId)
    {
        TypeId = typeId;
        SubscriptionTypeId = subscriptionTypeId;
        ServiceTypeId = serviceTypeId;
    }

    public Id TypeId { get; }
    public Id SubscriptionTypeId { get; }
    public Id ServiceTypeId { get; }
}
