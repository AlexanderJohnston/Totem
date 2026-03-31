using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Manages client creation per server. One instance per ServerName.
  /// </summary>
  public class ClientManagerTopic : Topic
  {
    readonly HashSet<KnownClient> _clients = new();

    static Id RouteFirst(NewClient e) => Id.From(e.ServerName);
    static Id Route(ClientCreated e) => Id.From(e.Client.ServerName);

    void Given(ClientCreated e)
    {
      _clients.Add(e.Client);
    }

    void When(NewClient command)
    {
      if (_clients.Any(c => string.Equals(c.JobNumber, command.JobNumber, StringComparison.OrdinalIgnoreCase)))
      {
        Then(new ClientAlreadyExists(command.JobName, command.JobNumber, command.ServerName));
      }
      else
      {
        var client = new KnownClient(command.JobName, command.JobNumber, Id.FromGuid(), command.ServerName);
        Then(new ClientCreated(client));
      }
    }
  }
}
