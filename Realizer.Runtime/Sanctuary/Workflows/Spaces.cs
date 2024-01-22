using Realizer.Messages.Sanctuary;
using Realizer.Runtime.Sanctuary.Events;

namespace Realizer.Runtime.Sanctuary.Workflows;
public class Spaces : Workflow
{
    public static Id Route(Entrance e) => e.PlaceId;
    public static Id Route(FirstToArrive e) => e.PlaceId;

    public void When(Entrance e) =>
        ThenEnqueue(new Arrive(e.PlaceId, e.NameId));

    public void When(FirstToArrive e)
    {
        ThenEnqueue(new Claim(e.PlaceId, e.NameId));
    }
}
