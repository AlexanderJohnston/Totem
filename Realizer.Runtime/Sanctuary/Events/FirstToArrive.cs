namespace Realizer.Runtime.Sanctuary.Events;

public class FirstToArrive : IEvent
{
    public FirstToArrive(Id placeId, Id nameId)
    {
        PlaceId = placeId;
        NameId = nameId;
    }

    public Id PlaceId { get; }
    public Id NameId { get; }
}
