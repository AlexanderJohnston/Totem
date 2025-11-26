using System.Collections.Concurrent;
using Totem.Core;
using Totem.InExternal.Services;
using Totem.InMemory;
using Totem.Workflows;

namespace Totem.InExternal;

public sealed class ExternalWorkflowStore : IWorkflowStore
{
    readonly ConcurrentDictionary<IWorkflowTransaction, ExternalWorkflowStream> _streamsByTransaction = new();
    readonly IAbstractEventStoreService _eventStoreService;
    readonly IServiceProvider _services;
    readonly IInMemoryWorkflowCommandBus _commandBus;
    readonly TotemJsonFormat _jsonFormat;
    readonly RuntimeMap _map;

    public ExternalWorkflowStore(
        IAbstractEventStoreService eventStoreService,
        IServiceProvider services,
        IInMemoryWorkflowCommandBus commandBus,
        TotemJsonFormat jsonFormat,
        RuntimeMap map)
    {
        _eventStoreService = eventStoreService;
        _services = services;
        _commandBus = commandBus;
        _jsonFormat = jsonFormat;
        _map = map;
    }

    public async Task<IWorkflowTransaction> StartTransactionAsync(IWorkflowContext<IEvent> context, CancellationToken cancellationToken)
    {
        var workflow = (IWorkflow) _services.GetRequiredService(context.WorkflowType.DeclaredType);

        if(workflow is ITimelineInit init)
        {
            init.TimelineId = context.WorkflowId;
        }

        var stream = new ExternalWorkflowStream(context.WorkflowType, context.WorkflowKey, _eventStoreService, _jsonFormat, _map);
        var version = await stream.LoadAsync(workflow, context, cancellationToken);

        context.WorkflowType.CallGivenIfDefined(workflow, context);

        var transaction = new WorkflowTransaction(this, context, workflow, version, cancellationToken);

        if(!_streamsByTransaction.TryAdd(transaction, stream))
        {
            throw new InvalidOperationException("Failed to register external workflow stream for transaction.");
        }

        return transaction;
    }

    public async Task CommitAsync(IWorkflowTransaction transaction, CancellationToken cancellationToken)
    {
        if(!_streamsByTransaction.TryRemove(transaction, out var stream))
        {
            throw new InvalidOperationException("Missing external workflow stream for transaction.");
        }

        var newCommands = await stream.CommitAsync(transaction, cancellationToken);

        if(newCommands.Count > 0)
        {
            _commandBus.Publish(newCommands);
        }
    }

    public Task RollbackAsync(IWorkflowTransaction transaction, CancellationToken cancellationToken)
    {
        _streamsByTransaction.TryRemove(transaction, out _);
        return Task.CompletedTask;
    }
}
