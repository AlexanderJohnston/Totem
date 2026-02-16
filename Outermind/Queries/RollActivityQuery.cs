using System;
using System.Collections.Generic;
using System.IO;
using Totem;
using Totem.Timeline;

namespace Outermind.Queries
{
  public class RollActivityQuery : Query
  {
    public class RollSession
    {
      public string RollPath { get; set; }
      public Id ProjectId { get; set; } = Id.From("Unknown");
      public DateTime FirstTouchUtc { get; set; }
      public DateTime LastTouchUtc { get; set; }
      public int TouchCount { get; set; }
    }

    public SortedDictionary<string, RollSession> RollsWorked { get; set; } = new SortedDictionary<string, RollSession>(StringComparer.OrdinalIgnoreCase);

    static Id RouteFirst(ScanDetected e) => e.UserId;
    static Id Route(ProjectClassified e) => e.UserId;

    void Given(ScanDetected e)
    {
      var rollPath = e.Scan?.FolderPath ?? string.Empty;
      if(string.IsNullOrWhiteSpace(rollPath))
        return;

      var touchedAtUtc = e.Scan != null && e.Scan.CompletedAtUtc != default
        ? e.Scan.CompletedAtUtc
        : e.UpdatedAtUtc;

      var key = Normalize(rollPath);

      if(!RollsWorked.TryGetValue(key, out var session))
      {
        session = new RollSession
        {
          RollPath = rollPath,
          FirstTouchUtc = touchedAtUtc
        };
        RollsWorked[key] = session;
      }

      session.LastTouchUtc = touchedAtUtc;
      session.TouchCount++;
    }

    void Given(ProjectClassified e)
    {
      var key = Normalize(e.FolderPath);

      if(RollsWorked.TryGetValue(key, out var session))
      {
        session.ProjectId = e.Project;
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
}
