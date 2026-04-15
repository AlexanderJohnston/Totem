using System;
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
      var filtered = new List<SmartScanRecord>();
      if (command.Scans.Count > 0)
      {
        foreach (var scan in command.Scans)
        {
          if (scan.LastModifiedBy != null && !scan.LastModifiedBy.Contains("Administrator"))
          {
            filtered.Add(scan);
          }
        }
        Then(new RegisterScans(filtered, command.RequestedAtUtc));
      }
      else
        Then(new RejectScans());
    }

    void When(RegisterScans command)
    {
      var snapshot = command.Scans == null ? new List<SmartScanRecord>() : new List<SmartScanRecord>(command.Scans);
      foreach (var scan in snapshot)
      {
        var path = (scan.FolderPath ?? string.Empty).AsSpan();
        var normalizedPath = TrimTrailingDirectorySeparators(path);
        var isRejectedPath = IsRejectedPath(path);

        // This may be the cause for some data loss. We originally skipped these paths because the DB could
        // not handle the load, and the streams were all long-lived. We switched to using TemporalUser keys {date}{username}
        // which seems to have mitigated this.
        //
        //var isCaptureOnePath =
        //  normalizedPath.Contains("captureone".AsSpan(), StringComparison.OrdinalIgnoreCase);

        //var shouldSkipPath = (
        //    normalizedPath.EndsWith("strips".AsSpan(), StringComparison.OrdinalIgnoreCase)
        //    || normalizedPath.EndsWith("previews".AsSpan(), StringComparison.OrdinalIgnoreCase)
        //    || normalizedPath.EndsWith("thumbs".AsSpan(), StringComparison.OrdinalIgnoreCase)
        //    || normalizedPath.EndsWith("thumb".AsSpan(), StringComparison.OrdinalIgnoreCase)
        //    || normalizedPath.Contains("qptemp".AsSpan(), StringComparison.OrdinalIgnoreCase)
        //  ) || isCaptureOnePath; // Include paths which contain junk files
        //if (shouldSkipPath)
        //{
        //  continue;
        //}

        if (isRejectedPath)
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

    static bool IsRejectedPath(ReadOnlySpan<char> path)
    {
      return path.Contains("3-indexing".AsSpan(), StringComparison.OrdinalIgnoreCase)
        || path.Contains("1-ip".AsSpan(), StringComparison.OrdinalIgnoreCase)
        || path.Contains("3-samy".AsSpan(), StringComparison.OrdinalIgnoreCase)
        || path.Contains("samy".AsSpan(), StringComparison.OrdinalIgnoreCase)
        || path.Contains("derivitives".AsSpan(), StringComparison.OrdinalIgnoreCase)
        || path.Contains("derivatives".AsSpan(), StringComparison.OrdinalIgnoreCase)
        || path.Contains("deliver".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    static ReadOnlySpan<char> TrimTrailingDirectorySeparators(ReadOnlySpan<char> path)
    {
      var end = path.Length;
      while (end > 0 && (path[end - 1] == '\\' || path[end - 1] == '/'))
      {
        end--;
      }

      return path[..end];
    }
  }
}
