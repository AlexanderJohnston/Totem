using Outermind;
using System;
using Totem;
using Totem.Timeline;

namespace Quantum.Topics.Clients
{
  /// <summary>
  /// Classifies ScanDetected events by matching against ClientProfileRegistry.
  /// New clients are added by registering a new ClientPathProfile.
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

      if (ClientProfileRegistry.TryMatch(path, segments, out var profile))
      {
        var owner = e.UserId;
        var changeType = e.Scan?.ChangeTag ?? string.Empty;
        Then(new ClientScanDetected(path, owner, changeType, profile.ClientPrefix));
      }
    }
  }
}
