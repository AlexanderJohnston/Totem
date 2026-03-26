using Outermind;
using System;
using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Quantum.Queries.Clients
{
  /// <summary>
  /// Read model: the definitive list of unique rolls for a pallet+box.
  /// Routed by "{Client}:{Pallet}:{Box}".
  /// </summary>
  public class BoxRollList : Query
  {
    public string Client { get; set; }
    public string Pallet { get; set; }
    public string Box { get; set; }
    public HashSet<RollEntry> Rolls { get; set; } = new();

    static Id RouteFirst(NewRollDiscovered e) =>
      Id.From($"{e.Client}:{e.Pallet}:{e.Box}");

    void Given(NewRollDiscovered e)
    {
      Client = e.Client;
      Pallet = e.Pallet;
      Box = e.Box;
      Rolls.Add(new RollEntry
      {
        Roll = e.Roll,
        FullPath = e.FullPath,
        FirstSeenUtc = Clock.Now
      });
    }
  }

  public class RollEntry : IEquatable<RollEntry>
  {
    public string Roll { get; set; }
    public string FullPath { get; set; }
    public DateTimeOffset FirstSeenUtc { get; set; }

    public bool Equals(RollEntry other)
    {
      if (other is null) return false;
      if (ReferenceEquals(this, other)) return true;
      return FullPath == other.FullPath;
    }

    public override int GetHashCode()
    {
      return FullPath != null ? FullPath.GetHashCode() : 0;
    }
  }
}
