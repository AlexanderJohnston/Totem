using System;
using Totem;
using Totem.Timeline;

namespace Outermind.Topics
{
  /// <summary>
  /// Classifies ScanDetected events by client and emits client-specific events.
  /// New clients are added here as additional cases.
  /// </summary>
  public class ClientClassifier : Topic
  {
    void When(ScanDetected e)
    {
      var path = e.Scan?.FolderPath;
      if (string.IsNullOrWhiteSpace(path))
        return;

      var segments = path.Split(
        new[] { '\\', '/' },
        StringSplitOptions.RemoveEmptyEntries);

      // Need at least 3 segments to extract client: server, share, client
      if (segments.Length < 3)
        return;

      var client = segments[2];

      if (client.StartsWith("NARA", StringComparison.OrdinalIgnoreCase))
      {
        var owner = e.UserId;
        var changeType = e.Scan?.ChangeTag ?? string.Empty;
        Then(new ScanForNARA(path, owner, changeType));
      }
    }
  }
}
