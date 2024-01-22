namespace Realizer.Runtime.Sanctuary.Events;

public class ClaimEstablished : IEvent
{
    public ClaimEstablished(Id objectId, Id ownerId)
    {
        ObjectId = objectId;
        OwnerId = ownerId;
    }

    public Id ObjectId { get; }
    public Id OwnerId { get; }
}
