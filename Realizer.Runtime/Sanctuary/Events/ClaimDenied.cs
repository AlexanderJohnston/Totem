namespace Realizer.Runtime.Sanctuary.Events;

public class ClaimDenied : IEvent
{
    public ClaimDenied(Id objectId, Id ownerId)
    {
        ObjectId = objectId;
        OwnerId = ownerId;
    }

    public Id ObjectId { get; }
    public Id OwnerId { get; }
}
