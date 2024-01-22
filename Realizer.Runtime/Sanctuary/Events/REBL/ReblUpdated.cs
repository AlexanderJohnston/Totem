namespace Realizer.Runtime.Sanctuary.Events.REBL;

public class ReblUpdated : IEvent
{
    public ReblUpdated(Id instanceId, string potentialCommandResult)
    {
        InstanceId = instanceId;
        PotentialCommandResult = potentialCommandResult;
    }

    public Id InstanceId { get; }
    public string PotentialCommandResult { get; }
}
