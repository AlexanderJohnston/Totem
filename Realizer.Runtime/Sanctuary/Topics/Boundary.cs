using Realizer.Messages.Sanctuary;
using Realizer.Runtime.Sanctuary.Events;

namespace Realizer.Runtime.Sanctuary.Topics;
public class Boundary : Topic
{
    public static Id Route(Enter command) => command.PlaceId;

    public Dictionary<string, List<string>> _places = new();

    public async Task When(Enter command, CancellationToken cancel)
    {
        var canGetName = Id.TryFromAny(command.Name, out var nameId);
        if (canGetName)
        {
            Update(command.Place, command.Name);
            Then(new Entrance(command.PlaceId, nameId));
        }
    }

    private void Update(string place, string name)
    {
        if (_places.Any(p => p.Key == place))
        {
            _places[place].Add(name);
        }
        else
        {
            _places.Add(place, new List<string>() { name });
        }
    }
}
