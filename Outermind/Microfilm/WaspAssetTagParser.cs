using System;
using System.Text.RegularExpressions;

namespace Outermind.Microfilm
{
  public static class WaspAssetTagParser
  {
    static readonly Regex CanonicalAssetPattern = new(
      @"^(?<job>.+?)\s*-\s*Box(?:\s+|-)(?<value>.*?)\s*$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex CanonicalRollPattern = new(
      @"^(?<box>.+?)\s*-\s*(?<roll>.*)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex AlternateRollPattern = new(
      @"^(?<job>.+?)\s*-\s*(?<box>Historian\s+Box|Tray|Phase\s+5|OS|Ship6|Film|Photo)\s*-\s*(?<roll>.+?)\s*$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex CompactBoxPattern = new(
      @"^(?<job>.+)-(?<box>\d+(?:\.\d+)?)\s*$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool StartsWithJobNumber(string assetId, string jobNumber)
    {
      if (string.IsNullOrWhiteSpace(assetId) || string.IsNullOrWhiteSpace(jobNumber))
      {
        return false;
      }

      var normalizedJobNumber = jobNumber.Trim();

      if (!assetId.StartsWith(normalizedJobNumber, StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }

      var delimiterPosition = normalizedJobNumber.Length;

      while (delimiterPosition < assetId.Length && char.IsWhiteSpace(assetId[delimiterPosition]))
      {
        delimiterPosition++;
      }

      return delimiterPosition == assetId.Length || assetId[delimiterPosition] == '-';
    }

    public static bool TryGetJobNumber(string assetId, out string jobNumber)
    {
      if (TryParseRoll(assetId, out jobNumber, out _, out _))
      {
        return true;
      }

      return TryParseBox(assetId, out jobNumber, out _);
    }

    public static bool TryParseBox(string assetId, out string jobNumber, out string boxName)
    {
      if (TryParseRoll(assetId, out _, out _, out _))
      {
        jobNumber = null;
        boxName = null;
        return false;
      }

      var canonicalMatch = CanonicalAssetPattern.Match(assetId ?? "");

      if (canonicalMatch.Success)
      {
        var value = canonicalMatch.Groups["value"].Value.Trim();
        var rollMatch = CanonicalRollPattern.Match(value);

        if (!rollMatch.Success || string.IsNullOrWhiteSpace(rollMatch.Groups["roll"].Value))
        {
          jobNumber = canonicalMatch.Groups["job"].Value.Trim();
          boxName = rollMatch.Success
            ? rollMatch.Groups["box"].Value.Trim()
            : value;

          return !string.IsNullOrWhiteSpace(jobNumber) && !string.IsNullOrWhiteSpace(boxName);
        }
      }

      var compactMatch = CompactBoxPattern.Match(assetId ?? "");

      if (compactMatch.Success)
      {
        jobNumber = compactMatch.Groups["job"].Value.Trim();
        boxName = compactMatch.Groups["box"].Value.Trim();
        return !string.IsNullOrWhiteSpace(jobNumber) && !string.IsNullOrWhiteSpace(boxName);
      }

      jobNumber = null;
      boxName = null;
      return false;
    }

    public static bool TryParseRoll(string assetId, out string jobNumber, out string boxName, out string rollName)
    {
      var canonicalMatch = CanonicalAssetPattern.Match(assetId ?? "");

      if (canonicalMatch.Success)
      {
        var rollMatch = CanonicalRollPattern.Match(canonicalMatch.Groups["value"].Value.Trim());

        if (rollMatch.Success && !string.IsNullOrWhiteSpace(rollMatch.Groups["roll"].Value))
        {
          jobNumber = canonicalMatch.Groups["job"].Value.Trim();
          boxName = rollMatch.Groups["box"].Value.Trim();
          rollName = rollMatch.Groups["roll"].Value.Trim();
          return !string.IsNullOrWhiteSpace(jobNumber) && !string.IsNullOrWhiteSpace(boxName);
        }
      }

      var alternateMatch = AlternateRollPattern.Match(assetId ?? "");

      if (alternateMatch.Success)
      {
        jobNumber = alternateMatch.Groups["job"].Value.Trim();
        boxName = NormalizeWhitespace(alternateMatch.Groups["box"].Value);
        rollName = alternateMatch.Groups["roll"].Value.Trim();
        return !string.IsNullOrWhiteSpace(jobNumber)
          && !string.IsNullOrWhiteSpace(boxName)
          && !string.IsNullOrWhiteSpace(rollName);
      }

      jobNumber = null;
      boxName = null;
      rollName = null;
      return false;
    }

    static string NormalizeWhitespace(string value) =>
      Regex.Replace(value?.Trim() ?? "", @"\s+", " ");
  }
}
