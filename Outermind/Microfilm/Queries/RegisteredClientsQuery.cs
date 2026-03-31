using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks clients registered on a server. Multi-instance, routed by ServerName.
  /// </summary>
  public class RegisteredClientsQuery : Query
  {
    public HashSet<KnownClient> Clients { get; set; } = new();

    static Id RouteFirst(ClientCreated e) => Id.From(e.Client.ServerName);

    void Given(ClientCreated e)
    {
      Clients.Add(e.Client);
    }
  }
}
