using System.Collections.Generic;
using Outermind;
using Outermind.SmartScanning;
using Totem.Timeline;

namespace Outermind.Topics
{
  public class SmartScanner : Topic
  {
    void When(UpdateScanRegistry command)
    {
      if (command.Scans.Count > 0)
        Then(new RegisterScans(command.Scans, command.RequestedAtUtc));
    }

    void When(RegisterScans command)
    {
      var snapshot = command.Scans == null ? new List<SmartScanRecord>() : new List<SmartScanRecord>(command.Scans);
      foreach (var scan in snapshot)
      {
        // We need to clean up the username or it will cause issues.
        // The backslash in names like CMGFX\ajohnston needs to be re-written as an underscore.
        var name = scan.LastModifiedBy;
        name = !string.IsNullOrWhiteSpace(name) ? name : "Unknown";
        name = name.ToString().Replace("CMGFX\\", "");
        name = name.ToString().Replace("Workflow_", "");
        var userId = Totem.Id.From(name);

        var path = scan.FolderPath ?? string.Empty;
        var pathLower = path.ToLowerInvariant();
        var isRejectedPath = pathLower.Contains("3-indexing") || pathLower.Contains("1-ip") || pathLower.Contains("3-samy");

        if (isRejectedPath)
        {
          Then(new ScanRejected(scan, userId, command.RequestedAtUtc));
        }
        else
        {
          // Emit a ScanDetected event for each scan
          Then(new ScanDetected(scan, userId, command.RequestedAtUtc));
        }
      }
    }
  }
}
