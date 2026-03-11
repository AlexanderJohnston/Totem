using System;
using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Queries
{
  public class TimeOnTaskQuery : Query
  {
    public class OffTaskWindow
    {
      public DateTime OffTaskBeganAtUtc { get; set; }
      public DateTime ResumedAtUtc { get; set; }
      public string LastFolderPath { get; set; }
      public string NextFolderPath { get; set; }
      public OffTaskKind Kind { get; set; }
      public TimeSpan Duration => ResumedAtUtc - OffTaskBeganAtUtc;
    }

    public List<OffTaskWindow> OffTaskWindows { get; set; } = new();

    static Id RouteFirst(TimeOffTask e) => e.TemporalUser;

    void Given(TimeOffTask e)
    {
      if (OffTaskWindows.Any(w => w.OffTaskBeganAtUtc == e.OffTaskBeganAtUtc && w.ResumedAtUtc == e.ResumedAtUtc))
        return;

      OffTaskWindows.Add(new OffTaskWindow
      {
        OffTaskBeganAtUtc = e.OffTaskBeganAtUtc,
        ResumedAtUtc = e.ResumedAtUtc,
        LastFolderPath = e.LastFolderPath,
        NextFolderPath = e.NextFolderPath,
        Kind = e.Kind
      });
    }
  }
}
