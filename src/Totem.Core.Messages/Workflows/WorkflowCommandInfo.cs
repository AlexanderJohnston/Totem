namespace Totem.Workflows;

public sealed class WorkflowCommandInfo : MessageInfo
{
    static readonly MessageInfoCache<WorkflowCommandInfo> _cache = new();

    WorkflowCommandInfo(Type declaredType, string queue) : base(declaredType) =>
        Queue = queue;

    public string Queue { get; }

    public const string DefaultQueue = "default";

    public static bool TryFrom(Type type, [NotNullWhen(true)] out WorkflowCommandInfo? info)
    {
        if(_cache.TryGetValue(type, out info))
        {
            return true;
        }

        if(type is not null && type.IsConcreteClass() && typeof(IWorkflowCommand).IsAssignableFrom(type))
        {
            var queue = type.GetCustomAttribute<WorkflowQueueAttribute>()?.Queue ?? DefaultQueue;

            info = new WorkflowCommandInfo(type, queue);

            _cache.Add(info);

            return true;
        }

        return false;
    }

    public static WorkflowCommandInfo From(Type type)
    {
        if(!TryFrom(type, out var info))
            throw new ArgumentException($"Expected type to be a public, non-abstract class implementing {typeof(IWorkflowCommand)} and optionally decorated with {typeof(WorkflowQueueAttribute)}", nameof(type));

        return info;
    }
}
