namespace Totem;

public abstract class Workflow : Timeline, IWorkflow
{
    public static readonly Id CorrelationIdPlaceholder = (Id) "12ff2fb1-5997-47bc-9a84-c5ca2cc68cf3";

    readonly ConcurrentQueue<WorkflowCommandEnvelope> _newCommands = new();

    public IReadOnlyList<WorkflowCommandEnvelope> GetNewCommands() =>
        _newCommands.ToList();

    protected void ThenEnqueue(WorkflowCommandEnvelope command)
    {
        if(HasErrors)
            throw new Exception($"Workflow {this} has one or more errors preventing {command.Command} from enqueueing");

        _newCommands.Enqueue(command);
    }

    protected void ThenEnqueue(IEnumerable<WorkflowCommandEnvelope> commands)
    {
        foreach(var command in commands)
        {
            ThenEnqueue(command);
        }
    }

    protected void ThenEnqueue(params WorkflowCommandEnvelope[] commands) =>
        ThenEnqueue(commands.AsEnumerable());

    protected void ThenEnqueue(IWorkflowCommand command, Id? correlationId = null, ClaimsPrincipal? principal = null) =>
        ThenEnqueue(new WorkflowCommandEnvelope(command, correlationId ?? CorrelationIdPlaceholder, principal));

    protected void ThenEnqueue(IEnumerable<IWorkflowCommand> commands, Id? correlationId = null, ClaimsPrincipal? principal = null) =>
        ThenEnqueue(commands.Select(command => new WorkflowCommandEnvelope(command, correlationId, principal)));

    protected void ThenEnqueue(Id correlationId, ClaimsPrincipal principal, params IWorkflowCommand[] commands) =>
        ThenEnqueue(commands.AsEnumerable(), correlationId, principal);

    protected void ThenEnqueue(Id correlationId, params IWorkflowCommand[] commands) =>
        ThenEnqueue(commands.AsEnumerable(), correlationId);

    protected void ThenEnqueue(ClaimsPrincipal principal, params IWorkflowCommand[] commands) =>
        ThenEnqueue(commands.AsEnumerable(), principal: principal);

    protected void ThenEnqueue(params IWorkflowCommand[] commands) =>
        ThenEnqueue(commands.AsEnumerable());
}
