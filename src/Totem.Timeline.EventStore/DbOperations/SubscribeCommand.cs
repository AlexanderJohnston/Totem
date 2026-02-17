using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;
using System.Text.Json.Nodes;
using Totem.Reflection;
using Totem.Runtime.Json;
using Totem.Timeline.Runtime;

namespace Totem.Timeline.EventStore.DbOperations
{
  /// <summary>
  /// Subscribes to the timeline of the hosted area
  /// </summary>
  internal class SubscribeCommand
  {
    readonly EventStoreContext _context;
    readonly ITimelineObserver _observer;

    internal SubscribeCommand(
      EventStoreContext context,
      ITimelineObserver observer)
    {
      _context = context;
      _observer = observer;
    }

    internal async Task<ResumeInfo> Execute()
    {
      var result = _context.Client.ReadStreamAsync(
        Direction.Backwards,
        TimelineStreams.Resume,
        StreamPosition.End,
        maxCount: 1);

      if(await result.ReadState == ReadState.StreamNotFound)
      {
        return ReadInitialResumeInfo();
      }

      var events = new System.Collections.Generic.List<ResolvedEvent>();
      await foreach(var e in result) events.Add(e);

      if(events.Count == 0)
      {
        return ReadInitialResumeInfo();
      }

      return await ReadResumeInfo(events[0].Event.Data.ToArray());
    }

    ResumeInfo ReadInitialResumeInfo() =>
      new ResumeInfo(new TimelineSubscription(_context, TimelinePosition.None, _observer));

    async Task<ResumeInfo> ReadResumeInfo(byte[] data)
    {
      var json = _context.Json.ToJsonNodeUtf8(data);

      var checkpoint = ReadCheckpoint(json["checkpoint"]);
      var routes = ReadResumeFlows(json["routes"].AsArray()).ToMany();
      var schedule = await ReadResumeSchedule(json["schedule"].AsArray());

      var subscription = new TimelineSubscription(_context, checkpoint, _observer);

      return new ResumeInfo(checkpoint, routes, schedule, subscription);
    }

    TimelinePosition ReadCheckpoint(JsonNode json) =>
      json == null ? TimelinePosition.None : new TimelinePosition(json.GetValue<long>());

    IEnumerable<FlowKey> ReadResumeFlows(JsonArray json)
    {
      foreach(var typeItem in json)
      {
        if(typeItem is JsonArray multiInstance)
        {
          var type = _context.Area.GetFlow(TypeName.From(multiInstance[0].GetValue<string>()));

          foreach(var idItem in multiInstance.Skip(1))
          {
            yield return FlowKey.From(type, Id.From(idItem.GetValue<string>()));
          }
        }
        else
        {
          yield return FlowKey.From(typeItem.GetValue<string>(), _context.Area);
        }
      }
    }

    async Task<Many<TimelinePoint>> ReadResumeSchedule(JsonArray json)
    {
      if(json.Count == 0)
      {
        return new Many<TimelinePoint>();
      }

      var schedule = json.Select(node => node.GetValue<long>()).ToMany();

      return await new ReadResumeScheduleCommand(_context, schedule).Execute();
    }
  }
}