using System;
using System.Text.RegularExpressions;

namespace Outermind.Microfilm
{
  public static class WaspAssetTagParser
  {
    static readonly Regex BoxPattern = new(@"^(?<job>.+?)-Box[\s-]+(?<box>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex RollPattern = new(@"^(?<job>.+?)-Box[\s-]+(?<box>.+?)\s*-\s*(?<roll>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool StartsWithJobNumber(string assetId, string jobNumber)
    {
      if (string.IsNullOrWhiteSpace(assetId) || string.IsNullOrWhiteSpace(jobNumber))
      {
        return false;
      }

      if (!assetId.StartsWith(jobNumber, StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }

      return assetId.Length == jobNumber.Length || assetId[jobNumber.Length] == '-';
    }

    public static bool TryGetJobNumber(string assetId, out string jobNumber)
    {
      if (TryParseRoll(assetId, out jobNumber, out _, out _))
      {
        return true;
      }

      if (TryParseBox(assetId, out jobNumber, out _))
      {
        return true;
      }

      var boxIndex = assetId?.IndexOf("-Box", StringComparison.OrdinalIgnoreCase) ?? -1;

      if (boxIndex > 0)
      {
        jobNumber = assetId.Substring(0, boxIndex);
        return !string.IsNullOrWhiteSpace(jobNumber);
      }

      jobNumber = null;
      return false;
    }

    public static bool TryParseBox(string assetId, out string jobNumber, out string boxName)
    {
      if (TryParseRoll(assetId, out _, out _, out _))
      {
        jobNumber = null;
        boxName = null;
        return false;
      }

      var match = BoxPattern.Match(assetId ?? "");

      if (match.Success)
      {
        jobNumber = match.Groups["job"].Value;
        boxName = match.Groups["box"].Value;
        return true;
      }

      jobNumber = null;
      boxName = null;
      return false;
    }

    public static bool TryParseRoll(string assetId, out string jobNumber, out string boxName, out string rollName)
    {
      var match = RollPattern.Match(assetId ?? "");

      if (match.Success)
      {
        jobNumber = match.Groups["job"].Value;
        boxName = match.Groups["box"].Value;
        rollName = match.Groups["roll"].Value;
        return true;
      }

      jobNumber = null;
      boxName = null;
      rollName = null;
      return false;
    }
  }
}
