using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;
using Totem.Timeline.Runtime;

namespace Totem.Timeline.EventStore.DbOperations
{
  /// <summary>
  /// Reads the <see cref="FlowResumeInfo"/> for a particular flow
  /// </summary>
  internal sealed class ReadFlowToResumeCommand
  {
    readonly Many<TimelinePoint> _points = new Many<TimelinePoint>();
    readonly EventStoreContext _context;
    readonly FlowKey _key;
    readonly string _routesStream;
    long? _areaCheckpoint;
    long _routesCheckpoint;
    int _batchIndex;

    internal ReadFlowToResumeCommand(EventStoreContext context, FlowKey key)
    {
      _context = context;
      _key = key;

      _routesStream = key.GetRoutesStream();
    }

    internal async Task<FlowResumeInfo> Execute()
    {
      var flow = await ReadFlow();

      switch(flow)
      {
        case FlowInfo.NotFound notFound:
          return await StartAndResume();
        case FlowInfo.Loaded loaded:
          return await Resume(loaded.Flow);
        case FlowInfo.Stopped stopped:
          throw new Exception($"Flow is stopped at {stopped.Position} with this error: {stopped.Error}");
        default:
          throw new NotSupportedException($"Unknown flow info type {flow.GetType()}");
      }
    }

    Task<FlowInfo> ReadFlow() =>
      new ReadFlowCommand(_context, _key).Execute();

    Task<FlowResumeInfo> StartAndResume()
    {
      var flow = _key.Type.New();

      FlowContext.Bind(flow, _key);

      return Resume(flow);
    }

    async Task<FlowResumeInfo> Resume(Flow flow)
    {
      _areaCheckpoint = flow.Context.CheckpointPosition.ToInt64OrNull();

      await ReadPoints();

      if(_points.Count == 0)
      {
        throw new Exception($"Flow was specified to resume but has no pending routes");
      }

      return new FlowResumeInfo(flow, _points);
    }

    async Task ReadPoints()
    {
      var lastRoute = await ReadLastRoute();

      if(lastRoute != null)
      {
        await ReadPoints(lastRoute.Value);
      }
    }

    async Task<ResolvedEvent?> ReadLastRoute()
    {
      var result = _context.Client.ReadStreamAsync(Direction.Backwards, _routesStream, StreamPosition.End, maxCount: 1, resolveLinkTos: true);

      if(await result.ReadState == ReadState.StreamNotFound)
      {
        return null;
      }

      var events = new List<ResolvedEvent>();
      await foreach(var e in result) events.Add(e);

      return events.Count > 0 ? events[0] : (ResolvedEvent?)null;
    }

    async Task ReadPoints(ResolvedEvent lastRoute)
    {
      if(_areaCheckpoint == null || _areaCheckpoint < (long)lastRoute.Event.EventNumber.ToUInt64())
      {
        AddPoint(lastRoute);

        while(await ReadNextBatch())
        {
          _batchIndex++;
        }
      }
    }

    void AddPoint(ResolvedEvent e)
    {
      _points.Write.Insert(0, _context.ReadAreaPoint(e));

      _routesCheckpoint = (long)e.Link.EventNumber.ToUInt64();
    }

    async Task<bool> ReadNextBatch()
    {
      if(_routesCheckpoint == 0)
      {
        return false;
      }

      var batch = await ReadBatch();

      foreach(var e in batch)
      {
        if((long)e.Event.EventNumber.ToUInt64() <= _areaCheckpoint)
        {
          return false;
        }

        AddPoint(e);
      }

      // If fewer results than requested, we've reached the end
      var batchSize = _key.Type.ResumeAlgorithm.GetNextBatchSize(_batchIndex);
      return batch.Count >= batchSize;
    }

    async Task<List<ResolvedEvent>> ReadBatch()
    {
      var batchSize = _key.Type.ResumeAlgorithm.GetNextBatchSize(_batchIndex);
      var startPos = new StreamPosition((ulong)(_routesCheckpoint - 1));

      var result = _context.Client.ReadStreamAsync(
        Direction.Backwards,
        _routesStream,
        startPos,
        maxCount: batchSize,
        resolveLinkTos: true);

      if(await result.ReadState == ReadState.StreamNotFound)
      {
        throw new Exception($"Unexpected result when reading {_routesStream} to resume: stream not found");
      }

      var events = new List<ResolvedEvent>();
      await foreach(var e in result) events.Add(e);
      return events;
    }
  }
}