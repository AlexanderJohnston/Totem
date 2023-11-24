namespace Totem.InMemory;

internal sealed class InMemoryWorkflowStream
{
    readonly List<IEventContext<IEvent>> _events = new();
    readonly WorkflowType _type;
    readonly RuntimeMap _map;
    TimelinePosition _version;

    internal InMemoryWorkflowStream(WorkflowType type, RuntimeMap map)
    {
        _type = type;
        _map = map;
    }

    internal TimelinePosition Load(IWorkflow workflow)
    {
        foreach(var e in _events)
        {
            _type.CallGivenIfDefined(workflow, e);

            _version = _version.Next();
        }

        return _version;
    }

    internal IReadOnlyList<WorkflowCommandEnvelope> Commit(IWorkflowTransaction transaction)
    {
        transaction.Context.WorkflowKey.CheckConcurrency(_version, transaction.Position);

        var persistedEvent = _map.CreateContext(transaction.Context.Envelope);

        _events.Add(persistedEvent);

        _version = _version.Next();

        return transaction.Workflow.GetNewCommands();
    }
}
