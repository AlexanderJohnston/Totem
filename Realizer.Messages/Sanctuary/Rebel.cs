namespace Realizer.Messages.Sanctuary;

public sealed class Rebel : IHttpCommand
{
    public Rebel(string command, string instance)
    {
        Command = command;
        Instance = instance;
        var canPlace = Id.TryFromAny(Instance, out var instanceId);
        if(canPlace)
        {
            InstanceId = instanceId;
        }
    }

    public string Command { get; }
    public string Instance { get; }
    public Id InstanceId { get; }
}
