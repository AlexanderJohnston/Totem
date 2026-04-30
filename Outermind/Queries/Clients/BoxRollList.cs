using Outermind;
using System;
using System.Collections.Generic;
using System.Linq;
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
    const int LegacyDrainBatchSize = 100;

    public string Client { get; set; }
    public string Pallet { get; set; }
    public string Box { get; set; }
    public HashSet<RollEntry> Rolls { get; set; } = new();
    public string RollsPrefix { get; set; }
    public Dictionary<string, long> RollsCompact { get; set; } = new();

    static Id RouteFirst(NewRollDiscovered e) =>
      Id.From($"{e.Client}:{e.Pallet}:{e.Box}");

    void Given(NewRollDiscovered e)
    {
      Client = e.Client;
      Pallet = e.Pallet;
      Box = e.Box;

      EnsurePrefix(e.FullPath);
      DrainLegacyRolls(LegacyDrainBatchSize);
      AddCompactRoll(e.FullPath, Clock.Now);
    }

    void EnsurePrefix(string fullPath)
    {
      Rolls ??= new HashSet<RollEntry>();

      if(!string.IsNullOrEmpty(RollsPrefix))
      {
        return;
      }

      var candidate = Rolls.FirstOrDefault()?.FullPath ?? fullPath;

      if(!string.IsNullOrEmpty(candidate))
      {
        RollsPrefix = ComputePrefix(candidate);
      }
    }

    void DrainLegacyRolls(int batchSize)
    {
      Rolls ??= new HashSet<RollEntry>();

      for(var i = 0; i < batchSize && Rolls.Count > 0; i++)
      {
        var legacyRoll = Rolls.First();
        Rolls.Remove(legacyRoll);

        if(legacyRoll != null)
        {
          AddCompactRoll(legacyRoll.FullPath, legacyRoll.FirstSeenUtc);
        }
      }
    }

    void AddCompactRoll(string fullPath, DateTimeOffset firstSeenUtc)
    {
      RollsCompact ??= new Dictionary<string, long>();

      var key = GetCompactKey(fullPath);

      if(!RollsCompact.ContainsKey(key))
      {
        RollsCompact[key] = firstSeenUtc.UtcDateTime.Ticks;
      }
    }

    string GetCompactKey(string fullPath)
    {
      if(!string.IsNullOrEmpty(fullPath)
        && !string.IsNullOrEmpty(RollsPrefix)
        && fullPath.StartsWith(RollsPrefix, StringComparison.Ordinal))
      {
        return fullPath[RollsPrefix.Length..];
      }

      return fullPath ?? string.Empty;
    }

    static string ComputePrefix(string fullPath)
    {
      var path = fullPath ?? string.Empty;
      var separator = path.LastIndexOf('\\');

      return separator >= 0
        ? path[..(separator + 1)]
        : string.Empty;
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
