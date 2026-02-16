using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Totem.Runtime.Json;
using Totem.Timeline.Client;

namespace Totem.Timeline.EventStore.Client
{
  /// <summary>
  /// A subscription to the client stream in EventStore
  /// </summary>
  internal sealed class ClientSubscription : IDisposable
  {
    readonly EventStoreContext _context;
    readonly IClientObserver _observer;
    StreamSubscription _timelineSubscription;
    StreamSubscription _clientSubscription;

    internal ClientSubscription(EventStoreContext context, IClientObserver observer)
    {
      _context = context;
      _observer = observer;
    }

    public void Dispose()
    {
      _timelineSubscription?.Dispose();
      _clientSubscription?.Dispose();
    }

    internal async Task Subscribe()
    {
      await SubscribeToTimeline();
      await SubscribeToClient();
    }

    async Task SubscribeToTimeline()
    {
      _timelineSubscription = await _context.Client.SubscribeToStreamAsync(
        TimelineStreams.Timeline,
        FromStream.End,
        eventAppeared: async (sub, e, ct) => await OnNextFromTimeline(e),
        subscriptionDropped: (sub, reason, error) => OnDropped(reason, error));
    }

    async Task SubscribeToClient()
    {
      _clientSubscription = await _context.Client.SubscribeToStreamAsync(
        TimelineStreams.Client,
        FromStream.End,
        eventAppeared: async (sub, e, ct) => await OnNextFromClient(e),
        subscriptionDropped: (sub, reason, error) => OnDropped(reason, error));
    }

    Task OnNextFromTimeline(ResolvedEvent e) =>
      _observer.OnNext(_context.ReadAreaPoint(e));

    void OnDropped(SubscriptionDroppedReason reason, Exception error) =>
      _observer.OnDropped(reason.ToString(), error);

    Task OnNextFromClient(ResolvedEvent e)
    {
      switch(e.Event.EventType)
      {
        case "timeline:CommandFailed":
          return OnNext(ReadEvent<CommandFailed>(e));
        case "timeline:QueryChanged":
          return OnNext(ReadEvent<QueryChanged>(e));
        case "timeline:QueryStopped":
          return OnNext(ReadEvent<QueryStopped>(e));
        default:
          return Task.CompletedTask;
      }
    }

    T ReadEvent<T>(ResolvedEvent e) =>
      _context.Json.FromJsonUtf8<T>(e.Event.Data.ToArray());

    Task OnNext(CommandFailed e) =>
      _observer.OnCommandFailed(e.CommandId, e.Error);

    Task OnNext(QueryChanged e) =>
      _observer.OnQueryChanged(ETagFrom(e.ETag));

    Task OnNext(QueryStopped e) =>
      _observer.OnQueryStopped(ETagFrom(e.ETag), e.Error);

    QueryETag ETagFrom(string etag) =>
      QueryETag.From(etag, _context.Area);
  }
}