using System;
using System.Threading.Tasks;
using EventStore.Client;
using Totem.Runtime;
using Totem.Runtime.Json;
using Totem.Timeline.Area;

namespace Totem.Timeline.EventStore
{
  /// <summary>
  /// The connection, JSON format, and area map in effect for the EventStore timeline
  /// </summary>
  public class EventStoreContext : Connection
  {
    public EventStoreContext(EventStoreClient client, IJsonFormat json, AreaMap area, TimeSpan? deadline)
    {
      Client = client;
      Json = json;
      Area = area;
      Deadline = deadline;
    }

    public readonly EventStoreClient Client;
    public readonly IJsonFormat Json;
    public readonly AreaMap Area;
    public readonly TimeSpan? Deadline;

    protected override Task Open()
    {
      Log.Info("Connected to EventStore via gRPC client");

      return base.Open();
    }

    protected override Task Close()
    {
      Client.Dispose();

      return base.Close();
    }
  }
}
