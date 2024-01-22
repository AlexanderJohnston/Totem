namespace Realizer.Messages.Sanctuary;
public sealed class Arrive : IWorkflowCommand
{
    public Arrive(Id placeId, Id nameId)
    {
        PlaceId = placeId;
        NameId = nameId;
    }

    public Id PlaceId { get; }
    public Id NameId { get; }
}
