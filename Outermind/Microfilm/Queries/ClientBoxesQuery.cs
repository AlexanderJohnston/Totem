using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks boxes belonging to a client. Multi-instance, routed by ClientId.
  /// </summary>
  public class ClientBoxesQuery : Query
  {
    public HashSet<KnownBox> Boxes { get; set; } = new();

    static Id RouteFirst(BoxCreated e) => e.Box.ClientId;

    void Given(BoxCreated e)
    {
      Boxes.Add(e.Box);
    }
  }
}
