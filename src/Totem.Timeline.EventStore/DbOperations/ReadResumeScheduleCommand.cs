using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;

namespace Totem.Timeline.EventStore.DbOperations
{
  /// <summary>
  /// Reads the events for which to set timers when resuming
  /// </summary>
  internal class ReadResumeScheduleCommand
  {
    readonly Many<TimelinePoint> _points = new Many<TimelinePoint>();
    readonly EventStoreContext _context;
    readonly ResumeAlgorithm _algorithm;
    readonly Many<long> _schedule;
    readonly long _scheduleFirst;
    readonly long _scheduleLast;
    long _readCheckpoint = -1;
    int _batchIndex;

    internal ReadResumeScheduleCommand(EventStoreContext context, Many<long> schedule)
    {
      _context = context;
      _schedule = schedule;

      _scheduleFirst = schedule.First();
      _scheduleLast = schedule.Last();

      // There is overhead in piping a resume algorithm from configuration. The default should work until
      // we experience otherwise.

      _algorithm = new ResumeAlgorithm();
    }

    internal async Task<Many<TimelinePoint>> Execute()
    {
      while(await ReadNextBatch())
      {
        _batchIndex++;
      }

      return _points;
    }

    async Task<bool> ReadNextBatch()
    {
      if(_readCheckpoint == 0)
      {
        return false;
      }

      var batch = await ReadBatch();

      if(batch == null || batch.Count == 0)
      {
        return false;
      }

      foreach(var e in batch)
      {
        _readCheckpoint = (long)e.Link.EventNumber.ToUInt64();

        var areaPosition = (long)e.Event.EventNumber.ToUInt64();

        if(areaPosition < _scheduleFirst)
        {
          return false;
        }

        if(areaPosition <= _scheduleLast && _schedule.Contains(areaPosition))
        {
          _points.Write.Insert(0, _context.ReadAreaPoint(e));
        }
      }

      // If fewer results than requested, we've reached the end
      var batchSize = _algorithm.GetNextBatchSize(_batchIndex);
      return batch.Count >= batchSize;
    }

    async Task<List<ResolvedEvent>> ReadBatch()
    {
      var startPos = _readCheckpoint < 0
        ? StreamPosition.End
        : new StreamPosition((ulong)_readCheckpoint);

      var result = _context.Client.ReadStreamAsync(
        Direction.Backwards,
        TimelineStreams.Schedule,
        startPos,
        maxCount: _algorithm.GetNextBatchSize(_batchIndex),
        resolveLinkTos: true);

      var readState = await result.ReadState;

      if(readState == ReadState.StreamNotFound)
      {
        return new List<ResolvedEvent>();
      }

      var events = new List<ResolvedEvent>();
      await foreach(var e in result) events.Add(e);
      return events;
    }
  }
}