using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.Client;
using Totem.Reflection;
using Totem.Runtime.Json;
using Totem.Timeline.Area;

namespace Totem.Timeline.EventStore
{
  /// <summary>
  /// Extends <see cref="EventStoreContext"/> with database operations
  /// </summary>
  internal static class EventStoreContextExtensions
  {
    internal static EventType GetEventType(this EventStoreContext context, Event e) =>
      context.Area.Events.Get(e.GetType());

    internal static byte[] ToJson(this EventStoreContext context, object value) =>
      value == null ? null : context.Json.ToJsonUtf8(value).ToBytes();

    //
    // Event data
    //

    internal static EventData GetAreaEventData(
      this EventStoreContext context,
      Event e,
      TimelinePosition cause,
      DateTimeOffset when,
      DateTimeOffset? whenOccurs,
      bool fromSchedule,
      Id eventId,
      Id commandId,
      Id userId,
      FlowKey topic,
      Many<FlowKey> routes)
    {
      var type = context.GetEventType(e);

      var metadata = new AreaEventMetadata
      {
        Cause = cause,
        When = when,
        WhenOccurs = whenOccurs,
        FromSchedule = fromSchedule,
        CommandId = commandId,
        UserId = userId,
        Topic = topic
      };

      metadata.ApplyRoutes(type, routes);

      return new EventData(
        eventId.IsUnassigned ? Uuid.NewUuid() : Uuid.FromGuid(Guid.Parse(eventId.ToString())),
        type.ToString(),
        context.ToJson(e),
        context.ToJson(metadata),
        "application/json");
    }

    internal static Many<EventData> GetNewEventData(
      this EventStoreContext context,
      TimelinePosition cause,
      FlowKey topic,
      Many<Event> newEvents) =>

      newEvents.ToMany(e => context.GetAreaEventData(
        e,
        cause,
        e.When,
        Event.Traits.WhenOccurs.Get(e),
        false,
        Event.Traits.EventId.Get(e),
        Event.Traits.CommandId.Get(e),
        Event.Traits.UserId.Get(e),
        topic,
        context.GetEventType(e).GetRoutes(e).ToMany()));

    internal static EventData GetScheduledEventData(this EventStoreContext context, TimelinePoint cause, DateTimeOffset now) =>
      context.GetAreaEventData(
        cause.Event,
        cause.Position,
        now,
        null,
        true,
        Id.FromGuid(),
        cause.CommandId,
        cause.UserId,
        null,
        cause.Type.GetRoutes(cause.Event).ToMany());

    internal static EventData GetCheckpointEventData(this EventStoreContext context, Flow flow) =>
      new EventData(
        Uuid.NewUuid(),
        "timeline:Checkpoint",
        context.ToJson(flow),
        context.ToJson(new CheckpointMetadata
        {
          Position = flow.Context.CheckpointPosition,
          ErrorPosition = flow.Context.ErrorPosition,
          ErrorMessage = flow.Context.ErrorMessage,
          IsDone = flow.Context.IsDone
        }),
        "application/json");

    internal static EventData GetClientEventData(this EventStoreContext context, Event e) =>
      new EventData(
        Uuid.NewUuid(),
        $"timeline:{e.GetType().Name}",
        context.ToJson(e),
        null,
        "application/json");

    //
    // Reads
    //

    internal static TimelinePoint ReadAreaPoint(this EventStoreContext context, ResolvedEvent e, AreaEventMetadata metadata)
    {
      var type = context.ReadEventType(e);

      return new TimelinePoint(
        new TimelinePosition((long)e.Event.EventNumber.ToUInt64()),
        metadata.Cause,
        type,
        metadata.When,
        metadata.WhenOccurs,
        Id.From(e.Event.EventId.ToGuid()),
        metadata.CommandId,
        metadata.UserId,
        metadata.Topic,
        metadata.GetRoutes(context.Area).ToMany(),
        () =>
        {
          var instance = context.ReadEvent(e, type);

          Event.Traits.When.Set(instance, metadata.When);
          Event.Traits.WhenOccurs.Set(instance, metadata.WhenOccurs);
          Event.Traits.CommandId.Set(instance, metadata.CommandId);
          Event.Traits.UserId.Set(instance, metadata.UserId);

          return instance;
        });
    }

    internal static TimelinePoint ReadAreaPoint(this EventStoreContext context, ResolvedEvent e) =>
      context.ReadAreaPoint(e, context.ReadAreaMetadata(e));

    internal static AreaEventMetadata ReadAreaMetadata(this EventStoreContext context, ResolvedEvent e) =>
      context.Json.FromJsonUtf8<AreaEventMetadata>(e.Event.Metadata.ToArray());

    internal static CheckpointMetadata ReadCheckpointMetadata(this EventStoreContext context, ResolvedEvent e) =>
      context.Json.FromJsonUtf8<CheckpointMetadata>(e.Event.Metadata.ToArray());

    internal static EventType ReadEventType(this EventStoreContext context, ResolvedEvent e) =>
      context.Area.Events.Get(TypeName.From(e.Event.EventType));

    static Event ReadEvent(this EventStoreContext context, ResolvedEvent e, EventType type) =>
      (Event) context.Json.FromJsonUtf8(e.Event.Data.ToArray(), type.DeclaredType);

    //
    // Appends
    //

    static Task<IWriteResult> AppendEvent(this EventStoreContext context, string stream, EventData data) =>
      context.Client.AppendToStreamAsync(stream, StreamState.Any, new[] { data });

    internal static Task<IWriteResult> AppendToTimeline(this EventStoreContext context, IEnumerable<EventData> data) =>
      context.Client.AppendToStreamAsync(TimelineStreams.Timeline, StreamState.Any, data);

    internal static Task<IWriteResult> AppendToTimeline(this EventStoreContext context, EventData data) =>
      context.AppendEvent(TimelineStreams.Timeline, data);

    internal static Task<IWriteResult> AppendToCheckpoint(this EventStoreContext context, Flow flow) =>
      context.AppendEvent(flow.Context.Key.GetCheckpointStream(), context.GetCheckpointEventData(flow));

    internal static Task<IWriteResult> AppendToClient(this EventStoreContext context, Event e) =>
      context.Client.AppendToStreamAsync(TimelineStreams.Client, StreamState.Any, new[] { context.GetClientEventData(e) });

    //
    // Metadata
    //

    internal static Task<IWriteResult> SetCheckpointStreamMetadata(this EventStoreContext context, Flow flow, StreamMetadata value) =>
      context.Client.SetStreamMetadataAsync(flow.Context.Key.GetCheckpointStream(), StreamState.Any, value);
  }
}