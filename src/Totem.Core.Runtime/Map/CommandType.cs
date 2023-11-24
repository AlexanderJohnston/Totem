namespace Totem.Map;

public sealed class CommandType : MessageType
{
    internal CommandType(CommandInfo info) : base(info)
    { }

    public new CommandInfo Info => (CommandInfo) base.Info;
    public TopicType Topic { get; internal set; } = null!;
    public TopicRouteMethod? Route { get; internal set; }
    public TopicWhenMethod When { get; internal set; } = null!;

    internal TopicRoute CallRoute(ICommandContext<ICommand> context) =>
        Route?.Call(context) ?? Topic.CallSingleInstanceRoute();
}
