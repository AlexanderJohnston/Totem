namespace Totem;

public interface IWorkflowContext<out TEvent> : IObserverContext<TEvent>
    where TEvent : IEvent
{
    TimelineKey WorkflowKey { get; }
    WorkflowType WorkflowType { get; }
    Id WorkflowId { get; }
}
