namespace Totem.Map.Summary;

public sealed class TopicRouteMethodSummary
{
    public TopicRouteMethodSummary(CommandParameterSummary parameter) =>
        Parameter = parameter;

    public CommandParameterSummary Parameter { get; }
}
