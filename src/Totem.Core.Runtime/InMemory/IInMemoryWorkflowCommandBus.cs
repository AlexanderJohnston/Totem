namespace Totem.InMemory;

public interface IInMemoryWorkflowCommandBus
{
    void Publish(IReadOnlyList<WorkflowCommandEnvelope> newCommands);
}
