namespace Totem.Workflows;

public interface IWorkflow : IEventObserver
{
    IReadOnlyList<WorkflowCommandEnvelope> GetNewCommands();
}
