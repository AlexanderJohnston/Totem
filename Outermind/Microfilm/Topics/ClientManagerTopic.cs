using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Maintains a per-server client set for client-local reactions.
  /// No longer owns client creation; that is handled by ServerManagerTopic.
  /// </summary>
  public class ClientManagerTopic : Topic
  {
    readonly HashSet<KnownClient> _clients = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ServerId;
    static Many<Id> Route(ClientReassigned e) =>
      new[] { e.PreviousServerId, e.Client.ServerId }.ToMany();

    void Given(ClientCreated e)
    {
      _clients.Add(e.Client);
    }

    void Given(ClientReassigned e)
    {
      _clients.RemoveWhere(c => c.ClientId == e.Client.ClientId);
      _clients.Add(e.Client);
    }
  }
}
