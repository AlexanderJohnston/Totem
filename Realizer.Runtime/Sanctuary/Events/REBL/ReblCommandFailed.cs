namespace Realizer.Runtime.Sanctuary.Events.REBL;

public class ReblCommandFailed : IEvent
{
    public ReblCommandFailed(string command, string msg)
    {
        Command = command;
        ExceptionMsg = msg;
    }

    public string Command { get; }
    public string ExceptionMsg { get; }
}
