using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Manages server registration. Single instance.
  /// </summary>
  public class ServerManagerTopic : Topic
  {
    readonly HashSet<KnownServer> _servers = new();

    void Given(ServerCreated e)
    {
      _servers.Add(e.Server);
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
  }
}
