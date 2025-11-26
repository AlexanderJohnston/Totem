namespace Totem.Workflows;

public interface IWorkflowTransaction
{
    IWorkflowContext<IEvent> Context { get; }
    IWorkflow Workflow { get; }
    TimelinePosition Position { get; }

    Task CommitAsync();
    Task RollbackAsync();
}
