using System.Security.Cryptography;
using System.Text;

namespace Realizer.Messages.Sanctuary;
public sealed class Enter : IHttpCommand
{
    public Enter(string place, string name)
    {
        Place = place;
        Name = name;
        var canPlace = Id.TryFromAny(place, out var placeId);
        if (canPlace)
        {
            PlaceId = placeId;
        }
    }

    public string Place { get; }
    public string Name { get; }
    public Id PlaceId { get; }
}
