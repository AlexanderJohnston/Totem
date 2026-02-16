using System.Threading.Tasks;
using EventStore.Client;
using Totem.Runtime;
using Totem.Timeline.Runtime;

namespace Totem.Timeline.EventStore
{
  /// <summary>
  /// A resumable EventStore subscription to the hosted timeline area
  /// </summary>
  public class TimelineSubscription : Connection
  {
    readonly EventStoreContext _context;
    readonly TimelinePosition _checkpoint;
    readonly ITimelineObserver _observer;
    StreamSubscription _subscription;

    public TimelineSubscription(
      EventStoreContext context,
      TimelinePosition checkpoint,
      ITimelineObserver observer)
    {
      _context = context;
      _checkpoint = checkpoint;
      _observer = observer;
    }

    protected override async Task Open()
    {
      var fromStream = _checkpoint.IsSome
        ? FromStream.After(new StreamPosition((ulong)_checkpoint.ToInt64OrNull().Value))
        : FromStream.Start;

      _subscription = await _context.Client.SubscribeToStreamAsync(
        TimelineStreams.Timeline,
        fromStream,
        eventAppeared: async (subscription, e, ct) =>
          await _observer.OnNext(_context.ReadAreaPoint(e)),
        subscriptionDropped: (subscription, reason, error) =>
          _observer.OnDropped(reason.ToString(), error));
    }

    protected override Task Close()
    {
      _subscription?.Dispose();

      return base.Close();
    }
  }
}