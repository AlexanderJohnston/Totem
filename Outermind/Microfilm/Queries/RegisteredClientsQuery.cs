using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks clients registered on a server. Multi-instance, routed by ServerId.
  /// </summary>
  public class RegisteredClientsQuery : Query
  {
    public HashSet<KnownClient> Clients { get; set; } = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ServerId;
    static Many<Id> Route(ClientReassigned e) =>
      new[] { e.PreviousServerId, e.Client.ServerId }.ToMany();

    void Given(ClientCreated e)
    {
      Clients.Add(e.Client);
    }

    void Given(ClientReassigned e)
    {
      Clients.RemoveWhere(c => c.ClientId == e.Client.ClientId);
      Clients.Add(e.Client);
    }
  }
}
