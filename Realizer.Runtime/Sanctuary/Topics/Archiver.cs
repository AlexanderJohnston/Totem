using Realizer.Messages.Sanctuary;
using Realizer.Runtime.Sanctuary.Events;

namespace Realizer.Runtime.Sanctuary.Topics;

public class Archiver : Topic
{
    public static Id Route(Claim command) => command.ObjectId;

    private Dictionary<Id, Id> _claims = new();

    public async Task When(Claim command, CancellationToken cancel)
    {
        if(ClaimExists(command.ObjectId))
        {
            Then(new ClaimDenied(command.ObjectId, command.OwnerId));
        }
        _claims.Add(command.ObjectId, command.OwnerId);
        Then(new ClaimEstablished(command.ObjectId, command.OwnerId));
    }

    // Check if claim already exists
    private bool ClaimExists(Id objectId) => _claims.ContainsKey(objectId);
}
