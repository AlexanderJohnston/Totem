namespace Totem;

[AttributeUsage(AttributeTargets.Class)]
public sealed class WorkflowQueueAttribute : Attribute
{
    public WorkflowQueueAttribute(string queue) =>
        Queue = !string.IsNullOrWhiteSpace(queue) ? queue : throw new ArgumentOutOfRangeException(nameof(queue));

    public string Queue { get; }
}
