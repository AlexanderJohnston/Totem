using System;
using System.Collections.Generic;

namespace Quantum.Topics.Clients
{
  /// <summary>
  /// Registry of all client path profiles. To add a new client,
  /// add a new ClientPathProfile entry to the Profiles list.
  /// </summary>
  public static class ClientProfileRegistry
  {
    public static readonly IReadOnlyList<ClientPathProfile> Profiles = new[]
    {
      new ClientPathProfile("NARA",          "frames"),
      new ClientPathProfile("NotreDameUniv", "0-copied"),
      new ClientPathProfile("DatabankOtis",  "0-verified"),
      new ClientPathProfile("Madison",       "qc complete"),
    };

    /// <summary>
    /// Finds the first matching profile for a scan path.
    /// Matches on client prefix at segments[2] and final output signal keyword anywhere in the path.
    /// </summary>
    public static bool TryMatch(string path, string[] segments, out ClientPathProfile matched)
    {
      matched = null;

      if (segments.Length < 3)
        return false;

      var client = segments[2];
      var lowerPath = path.ToLower();

      foreach (var profile in Profiles)
      {
        if (client.StartsWith(profile.ClientPrefix, StringComparison.OrdinalIgnoreCase)
            && lowerPath.Contains(profile.FinalOutputSignal.ToLower()))
        {
          matched = profile;
          return true;
        }
      }

      return false;
    }
  }
}
