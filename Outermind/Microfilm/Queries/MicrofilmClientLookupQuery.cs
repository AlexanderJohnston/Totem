using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks known server and client identifiers for API validation.
  /// </summary>
  public class MicrofilmClientLookupQuery : Query
  {
    public HashSet<string> ServerIds { get; set; } = new();
    public Dictionary<string, KnownClient> ClientsById { get; set; } = new();

    void Given(ServerCreated e)
    {
      ServerIds.Add(e.Server.ServerId.ToString());
    }

    void Given(ClientCreated e)
    {
      ServerIds.Add(e.Client.ServerId.ToString());
      ClientsById[e.Client.ClientId.ToString()] = e.Client;
    }

    void Given(ClientReassigned e)
    {
      ServerIds.Add(e.Client.ServerId.ToString());
      ClientsById[e.Client.ClientId.ToString()] = e.Client;
    }

    public bool HasServer(Id serverId) =>
      ServerIds.Contains(serverId.ToString());

    public bool HasClient(Id clientId) =>
      ClientsById.ContainsKey(clientId.ToString());
  }
}
