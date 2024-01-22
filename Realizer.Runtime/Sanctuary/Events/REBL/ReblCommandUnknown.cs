namespace Realizer.Runtime.Sanctuary.Events.REBL;

public class ReblCommandUnknown : IEvent
{
    public ReblCommandUnknown(string instance, Id instanceId)
    {
        Instance = instance;
        InstanceId = instanceId;
    }

    public string Instance { get; }
    public Id InstanceId { get; }
}
