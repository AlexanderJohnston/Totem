namespace Realizer.Runtime.Sanctuary.Events;
public class Entrance : IEvent
{
    public Entrance(Id placeId, Id nameId)
    {
        PlaceId = placeId;
        NameId = nameId;
    }

    public Id PlaceId { get; }
    public Id NameId { get; }
}
