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
      if(command.Scans.Count > 0)
        Then(new RegisterScans(command.Scans, command.RequestedAtUtc));
      else
        Then(new RejectScans());
    }

    void When(RegisterScans command)
    {
      var snapshot = command.Scans == null ? new List<SmartScanRecord>() : new List<SmartScanRecord>(command.Scans);
      foreach(var scan in snapshot)
      {
        var path = scan.FolderPath ?? string.Empty;
        var pathLower = path.ToLowerInvariant();
        var isRejectedPath = pathLower.Contains("3-indexing") || pathLower.Contains("1-ip") || pathLower.Contains("3-samy");

        if(isRejectedPath)
        {
          var name = scan.LastModifiedBy;
          var userId = Totem.Id.From(name);
          Then(new ScanRejected(scan, userId, command.RequestedAtUtc));
        }
        else
        {
          // We need to clean up the username or it will cause issues.
          // The backslash in names like CMGFX\ajohnston needs to be re-written as an underscore.
          var name = scan.LastModifiedBy;
          name = !string.IsNullOrWhiteSpace(name) ? name : "Unknown";
          name = name.ToString().Replace("CMGFX\\", "");
          name = name.ToString().Replace("Workflow_", "");
          var userId = Totem.Id.From(name);

          // we need to build a key like {YEAR}{MONTH}{DAY}_{NAME} based on the starting UTC.
          var date = scan.StartedAtUtc;
          var temporalLookupKey = Totem.Id.From($"{date.Year}{date.Month:D2}{date.Day:D2}" + userId);

          // Emit a ScanDetected event for each scan
          Then(new ScanDetected(scan, temporalLookupKey, userId, command.RequestedAtUtc));
        }
      }
    }
  }
}
