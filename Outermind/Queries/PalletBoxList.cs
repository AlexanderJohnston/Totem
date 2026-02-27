using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Queries
{
  /// <summary>
  /// Navigation query: lists all boxes discovered for a client+pallet.
  /// Routed by "{Client}:{Pallet}". Stores only box names.
  /// </summary>
  public class PalletBoxList : Query
  {
    public HashSet<string> Boxes { get; set; } = new();

    static Id RouteFirst(NewRollDiscovered e) =>
      Id.From($"{e.Client}:{e.Pallet}");

    void Given(NewRollDiscovered e)
    {
      Boxes.Add(e.Box);
    }
  }
}
