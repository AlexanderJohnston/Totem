using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Outermind.SmartScanning;
using Totem;
using Totem.Timeline;
using System.IO;

namespace Outermind.Queries
{
  public class ScanTracker : Query
  {
    public class TrackedScan
    {
      public SmartScanRecord Scan { get; set; } = new();
      public FolderKind Kind { get; set; } = FolderKind.Unknown;
      public string Share { get; set; } = "Unknown";
      public Id Project { get; set; } = Id.From("Unknown");
    }

    public Dictionary<string, TrackedScan> KnownScans { get; set; } = new Dictionary<string, TrackedScan>();

    static Id RouteFirst(ScanDetected e) => e.UserId;
    static Id Route(FolderClassified e) => e.UserId;
    static Id Route(ShareClassified e) => e.UserId;
    static Id Route(ProjectClassified e) => e.UserId;

    void Given(ScanDetected e)
    {

      var key = Normalize(e.Scan.FolderPath);
      if(KnownScans == null)
      {
        KnownScans = new Dictionary<string, TrackedScan>();
        KnownScans[key] = new TrackedScan();
      }

      if(!KnownScans.TryGetValue(key, out var tracked))
      {
        tracked = new TrackedScan();
        KnownScans[key] = tracked;
      }
      else
      {
        KnownScans[key].Scan.FileCount += e.Scan.FileCount;
        KnownScans[key].Scan.LastModifiedBy = e.Scan.LastModifiedBy;
        KnownScans[key].Scan.ChangeTag = e.Scan.ChangeTag;
        KnownScans[key].Scan.StartedAtUtc = e.Scan.StartedAtUtc;
        KnownScans[key].Scan.CompletedAtUtc = e.Scan.CompletedAtUtc;
      }
    }

    void Given(FolderClassified e)
    {
      var key = Normalize(e.Folder.FolderPath);

      if(KnownScans.TryGetValue(key, out var tracked))
      {
        tracked.Kind = e.Folder.Kind;
      }
    }

    void Given(ShareClassified e)
    {
      var key = Normalize(e.FolderPath);

      if(KnownScans.TryGetValue(key, out var tracked))
      {
        tracked.Share = string.IsNullOrWhiteSpace(e.Share) ? "Unknown" : e.Share;
      }
    }

    void Given(ProjectClassified e)
    {
      var key = Normalize(e.FolderPath);

      if(KnownScans.TryGetValue(key, out var tracked))
      {
        tracked.Project = string.IsNullOrWhiteSpace(e.Project.ToString()) ? Id.From("Unknown") : e.Project;
      }
    }

    static string Normalize(string path)
    {
      try
      {
        return Path.GetFullPath(path);
      }
      catch
      {
        return path;
      }
    }
  }

  public class ScanSettingsQuery : Query
  {
    public Dictionary<Id, Dictionary<string, string>> SettingStore = new();

    void Given(SettingsUpdated e)
    {
      SettingStore[e.Roll] = e.Settings;
    }
  }
}
