using Outermind;
using System;
using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Quantum.Topics.Clients
{
  /// <summary>
  /// Tracks unique rolls per pallet+box. Routed by "{Client}:{Pallet}:{Box}".
  /// Deduplicates rolls detected multiple times at the same location.
  /// </summary>
  public class BoxInventory : Topic
  {
    readonly HashSet<string> _knownRolls = new(StringComparer.OrdinalIgnoreCase);

    static Id RouteFirst(RollPathDetected e) =>
      Id.From($"{e.Client}:{e.Pallet}:{e.Box}");

    static Id Route(NewRollDiscovered e) =>
      Id.From($"{e.Client}:{e.Pallet}:{e.Box}");

    void Given(NewRollDiscovered e) =>
      _knownRolls.Add(e.Roll);

    void When(RollPathDetected e)
    {
      if (_knownRolls.Contains(e.Roll))
      {
        return;
      }
      else
      {
        Then(new NewRollDiscovered(e.Client, e.Pallet, e.Box, e.Roll, e.FullPath));
      }
    }
  }
}
