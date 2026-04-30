using System;
using System.Collections.Generic;
using System.IO;

namespace Quantum.Queries.Clients
{
  public class BoxRollListResponse
  {
    public string Client { get; set; }
    public string Pallet { get; set; }
    public string Box { get; set; }
    public List<RollEntry> Rolls { get; set; } = new();

    public static BoxRollListResponse From(BoxRollList query)
    {
      var legacyRolls = query.Rolls ?? new HashSet<RollEntry>();
      var compactRolls = query.RollsCompact ?? new Dictionary<string, long>();
      var response = new BoxRollListResponse
      {
        Client = query.Client,
        Pallet = query.Pallet,
        Box = query.Box
      };

      var seenPaths = new HashSet<string>(StringComparer.Ordinal);

      foreach(var legacyRoll in legacyRolls)
      {
        if(legacyRoll != null && seenPaths.Add(legacyRoll.FullPath ?? string.Empty))
        {
          response.Rolls.Add(new RollEntry
          {
            Roll = legacyRoll.Roll,
            FullPath = legacyRoll.FullPath,
            FirstSeenUtc = legacyRoll.FirstSeenUtc
          });
        }
      }

      foreach(var compactRoll in compactRolls)
      {
        var fullPath = ExpandFullPath(query.RollsPrefix, compactRoll.Key);

        if(seenPaths.Add(fullPath ?? string.Empty))
        {
          response.Rolls.Add(new RollEntry
          {
            Roll = GetRollName(fullPath),
            FullPath = fullPath,
            FirstSeenUtc = new DateTimeOffset(compactRoll.Value, TimeSpan.Zero)
          });
        }
      }

      return response;
    }

    static string ExpandFullPath(string prefix, string key)
    {
      if(string.IsNullOrEmpty(key))
      {
        return prefix ?? string.Empty;
      }

      if(Path.IsPathRooted(key) || string.IsNullOrEmpty(prefix))
      {
        return key;
      }

      return $"{prefix}{key}";
    }

    static string GetRollName(string fullPath)
    {
      if(string.IsNullOrEmpty(fullPath))
      {
        return string.Empty;
      }

      var trimmedPath = fullPath.TrimEnd('\\');
      return Path.GetFileName(trimmedPath);
    }
  }
}
