using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Totem;
using Totem.Timeline;

namespace Outermind.Queries
{
  public class RollActivityQuery : Query
  {
    // String table for prefixes to avoid repeating long UNC roots per entry.
    public List<string> PathPrefixes { get; set; } = new List<string>();

    // Each entry: "{prefixIndex}|{relativePath}" where prefix is \\server\share\project\oneMoreNode\
    public HashSet<string> FoldersWorked { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    static Id RouteFirst(ScanDetected e) => e.TemporalUser;

    void Given(ScanDetected e)
    {
      var rollPath = e.Scan?.FolderPath ?? string.Empty;
      if (string.IsNullOrWhiteSpace(rollPath))
        return;

      var segments = rollPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
      if (!rollPath.StartsWith(@"\\", StringComparison.Ordinal) || segments.Length < 4)
        return;

      // Prefix rule: \\server\share\project\oneMoreNode\
      var prefix = $@"\\{segments[0]}\{segments[1]}\{segments[2]}\{segments[3]}\";

      var prefixIndex = -1;
      for (var i = 0; i < PathPrefixes.Count; i++)
      {
        if (string.Equals(PathPrefixes[i], prefix, StringComparison.OrdinalIgnoreCase))
        {
          prefixIndex = i;
          break;
        }
      }

      if (prefixIndex < 0)
      {
        prefixIndex = PathPrefixes.Count;
        PathPrefixes.Add(prefix);
      }

      var relativePath = segments.Length > 4
        ? string.Join("\\", segments, 4, segments.Length - 4)
        : string.Empty;

      FoldersWorked.Add($"{prefixIndex}|{relativePath}");
    }
  }
}
