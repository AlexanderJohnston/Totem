using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Queries
{
  /// <summary>
  /// Navigation query: lists all pallets discovered for a client.
  /// Routed by "{Client}". Stores only pallet names.
  /// </summary>
  public class ClientPalletList : Query
  {
    public HashSet<string> Pallets { get; set; } = new();

    static Id RouteFirst(NewRollDiscovered e) => Id.From(e.Client);

    void Given(NewRollDiscovered e)
    {
      Pallets.Add(e.Pallet);
    }
  }
}
