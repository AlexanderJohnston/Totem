using System.Collections.Generic;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks all known servers. Single instance.
  /// </summary>
  public class ServerQuery : Query
  {
    public HashSet<KnownServer> Servers { get; set; } = new();

    void Given(ServerCreated e)
    {
      Servers.Add(e.Server);
    }
  }
}
