namespace Totem.Map.Summary;

public sealed class NotificationHandlerSummary
{
    public NotificationHandlerSummary(Id typeId, Id notificationTypeId, Id serviceTypeId)
    {
        TypeId = typeId;
        NotificationTypeId = notificationTypeId;
        ServiceTypeId = serviceTypeId;
    }

    public Id TypeId { get; }
    public Id NotificationTypeId { get; }
    public Id ServiceTypeId { get; }
}
