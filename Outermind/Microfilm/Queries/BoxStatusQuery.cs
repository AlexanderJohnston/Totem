using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks a single box and its rolls. Multi-instance, routed by BoxId.
  /// </summary>
  public class BoxStatusQuery : Query
  {
    public KnownBox Box { get; set; }
    public HashSet<KnownRoll> Rolls { get; set; } = new();

    static Id RouteFirst(BoxCreated e) => e.Box.BoxId;
    static Id Route(RollCreated e) => e.Roll.BoxId;

    void Given(BoxCreated e)
    {
      Box = e.Box;
    }

    void Given(RollCreated e)
    {
      Rolls.Add(e.Roll);
    }
  }
}
