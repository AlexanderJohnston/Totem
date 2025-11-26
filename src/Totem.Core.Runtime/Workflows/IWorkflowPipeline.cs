namespace Totem.Workflows;

public interface IWorkflowPipeline
{
    Task<IWorkflowContext<IEvent>> RunAsync(EventEnvelope envelope, ObserverRoute route, CancellationToken cancellationToken);
}
