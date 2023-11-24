namespace Totem.Topics;

public sealed class TopicRoute
{
    public TopicRoute(TopicType topicType, Id topicId)
    {
        TopicType = topicType;
        TopicId = topicId;
    }

    public TopicType TopicType { get; }
    public Id TopicId { get; }

    public override string ToString() =>
        $"{TopicType}.{TopicId.ToShortString()}";
}
