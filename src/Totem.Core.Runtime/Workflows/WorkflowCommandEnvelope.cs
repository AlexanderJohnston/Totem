namespace Totem.Workflows;

public sealed class WorkflowCommandEnvelope : MessageEnvelope
{
    public WorkflowCommandEnvelope(IWorkflowCommand command, EnvelopeInfo info) : base(info)
    {
        Command = command;
        CommandInfo = WorkflowCommandInfo.From(command.GetType());
    }

    public WorkflowCommandEnvelope(IWorkflowCommand command, Id? correlationId = null, ClaimsPrincipal? principal = null)
        : this(command, new EnvelopeInfo(Id.NewId(), correlationId, principal))
    { }

    public IWorkflowCommand Command { get; }
    public WorkflowCommandInfo CommandInfo { get; }
    public Type CommandType => CommandInfo.DeclaredType;
    public Id CommandId => Info.MessageId;
    public string Queue => CommandInfo.Queue;

    public CommandEnvelope ToCommandEnvelope() =>
        new(Command, Info);
}
