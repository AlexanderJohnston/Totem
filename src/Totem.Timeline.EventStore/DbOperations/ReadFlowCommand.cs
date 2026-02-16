using System;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;
using Totem.Runtime.Json;
using Totem.Timeline.Runtime;

namespace Totem.Timeline.EventStore.DbOperations
{
  /// <summary>
  /// Reads the <see cref="FlowInfo"/> for a particular flow
  /// </summary>
  internal sealed class ReadFlowCommand
  {
    readonly EventStoreContext _context;
    readonly FlowKey _key;
    ResolvedEvent _checkpoint;
    CheckpointMetadata _metadata;

    internal ReadFlowCommand(EventStoreContext context, FlowKey key)
    {
      _context = context;
      _key = key;
    }

    internal async Task<FlowInfo> Execute()
    {
      var stream = _key.GetCheckpointStream();

      var result = _context.Client.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, maxCount: 1);

      if(await result.ReadState == ReadState.StreamNotFound)
      {
        return new FlowInfo.NotFound();
      }

      var events = new System.Collections.Generic.List<ResolvedEvent>();
      await foreach(var e in result) events.Add(e);

      if(events.Count == 0)
      {
        return new FlowInfo.NotFound();
      }

      _checkpoint = events[0];
      _metadata = _context.ReadCheckpointMetadata(_checkpoint);

      return ReadFlow();
    }

    FlowInfo ReadFlow()
    {
      if(_metadata.IsDone)
      {
        return new FlowInfo.NotFound();
      }
      else if(_metadata.ErrorPosition.IsSome)
      {
        return new FlowInfo.Stopped(_metadata.ErrorPosition, _metadata.ErrorMessage);
      }
      else
      {
        return new FlowInfo.Loaded(LoadFlow());
      }
    }

    Flow LoadFlow()
    {
      var flow = ReadInstance();

      FlowContext.Bind(flow, _key, _metadata.Position, TimelinePosition.None);

      return flow;
    }

    Flow ReadInstance() =>
      (Flow) _context.Json.FromJsonUtf8(_checkpoint.Event.Data.ToArray(), _key.Type.DeclaredType);
  }
}