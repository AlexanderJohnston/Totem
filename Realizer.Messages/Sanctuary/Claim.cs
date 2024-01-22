namespace Realizer.Messages.Sanctuary;

public sealed class Claim : IWorkflowCommand
{
    public Claim(Id objectId, Id ownerId)
    {
        ObjectId = objectId;
        OwnerId = ownerId;
    }

    public Id ObjectId { get; }
    public Id OwnerId { get; }
}
