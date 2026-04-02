using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Manages server and client registration. Single instance.
  /// </summary>
  public class ServerManagerTopic : Topic
  {
    readonly HashSet<KnownServer> _servers = new();
    readonly HashSet<KnownClient> _clients = new();

    void Given(ServerCreated e)
    {
      _servers.Add(e.Server);
    }

    void Given(ClientCreated e)
    {
      _clients.Add(e.Client);
    }

    void Given(ClientReassigned e)
    {
      _clients.RemoveWhere(c => c.ClientId == e.Client.ClientId);
      _clients.Add(e.Client);
    }

    void When(NewServer command)
    {
      if (_servers.Any(s => string.Equals(s.ServerName, command.ServerName, StringComparison.OrdinalIgnoreCase)))
      {
        Then(new ServerAlreadyExists(command.ServerName));
      }
      else
      {
        var server = new KnownServer(command.ServerName, Id.FromGuid());
        Then(new ServerCreated(server));
      }
    }

    void When(NewClient command)
    {
      if (!_servers.Any(s => s.ServerId == command.ServerId))
      {
        Then(new ServerNotRecognized(command.ServerId));
      }
      else if (_clients.Any(c => string.Equals(c.JobNumber, command.JobNumber, StringComparison.OrdinalIgnoreCase)))
      {
        Then(new ClientAlreadyExists(command.JobName, command.JobNumber, command.ServerId));
      }
      else
      {
        var client = new KnownClient(command.JobName, command.JobNumber, Id.FromGuid(), command.ServerId);
        Then(new ClientCreated(client));
      }
    }

    void When(ChangeClientAssignment command)
    {
      var client = _clients.FirstOrDefault(c => c.ClientId == command.ClientId);

      if (client == null)
      {
        Then(new ClientNotRecognized(command.ClientId));
      }
      else if (!_servers.Any(s => s.ServerId == command.ServerId))
      {
        Then(new ServerNotRecognized(command.ServerId));
      }
      else if (client.ServerId == command.ServerId)
      {
        Then(new ClientAlreadyAssignedToServer(client, command.ServerId));
      }
      else
      {
        var previousServerId = client.ServerId;
        var updated = new KnownClient(client.JobName, client.JobNumber, client.ClientId, command.ServerId);
        Then(new ClientReassigned(updated, previousServerId));
      }
    }
  }
}
