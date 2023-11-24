namespace Totem.Map.Summary;

public sealed class CommandSummary
{
    public CommandSummary(Id typeId, Id topicTypeId, TopicRouteMethodSummary? route, TopicWhenMethodSummary when)
    {
        TypeId = typeId;
        TopicTypeId = topicTypeId;
        Route = route;
        When = when;
    }

    public Id TypeId { get; }
    public Id TopicTypeId { get; }
    public TopicRouteMethodSummary? Route { get; }
    public TopicWhenMethodSummary When { get; }
}
