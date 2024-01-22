using Realizer.Messages.Sanctuary;
using Realizer.Runtime.Sanctuary.Events;
using REBL.Commands;

namespace Realizer.Runtime.Sanctuary.Topics;

public class Sanctuary : Topic
{
    public static Id Route(Arrive command) => command.PlaceId;
    public static Id Route(FirstToArrive command) => command.PlaceId;

    public List<Id> People = new();
    public List<Id> Guests = new();
    public Id Owner;

    public void Given(Arrive e)
    {
        if (!FirstArrival())
        {
            Guests.Add(e.NameId);
        }
        People.Add(e.NameId);
    }

    public void Given(FirstToArrive e)
    {
        if(FirstArrival())
        {
            Owner = e.NameId;
        }
        People.Add(e.NameId);
    }

    public async Task When(Arrive command, CancellationToken cancel)
    {
        if (FirstArrival())
        {
            Then(new FirstToArrive(command.PlaceId, command.NameId));
        }
    }

    private bool FirstArrival() => People.Count == 0;

    // Check if object id is in list of people
    private bool PersonExists(Id nameId) => People.Contains(nameId);
}
