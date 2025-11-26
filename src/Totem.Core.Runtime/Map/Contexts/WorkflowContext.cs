namespace Totem.Map.Contexts;

internal sealed class WorkflowContext<TEvent> : ObserverContext<TEvent>, IWorkflowContext<TEvent>
    where TEvent : IEvent
{
    internal WorkflowContext(EventEnvelope envelope, EventType eventType, ObserverRoute route)
        : base(envelope, eventType, route.Observation)
    {
        WorkflowKey = new(route.Observer.DeclaredType, route.ObserverId);
        WorkflowType = (WorkflowType) route.Observer;
        WorkflowId = route.ObserverId;
    }

    public TimelineKey WorkflowKey { get; }
    public WorkflowType WorkflowType { get; }
    public Id WorkflowId { get; }

    public override string ToString() =>
        $"{base.ToString()} => {WorkflowKey}";
}
