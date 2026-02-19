using System;
using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Topics
{
  public class ShiftScheduleTopic : Topic
  {
    static readonly TimeSpan OffTaskThreshold = TimeSpan.FromMinutes(5);
    static readonly TimeSpan ShiftBoundary = TimeSpan.FromHours(8);

    class UserActivity
    {
      public DateTime LastTouchedAtUtc;
      public string LastFolderPath;
    }

    readonly Dictionary<Id, UserActivity> _userActivity = new();

    void When(ScanDetected e)
    {
      var user = e.UserId;
      var touchedAtUtc = e.Scan != null && e.Scan.CompletedAtUtc != default
        ? e.Scan.CompletedAtUtc
        : e.UpdatedAtUtc;
      var currentFolder = e.Scan?.FolderPath ?? string.Empty;

      if(_userActivity.TryGetValue(user, out var activity))
      {
        var gap = touchedAtUtc - activity.LastTouchedAtUtc;

        if(gap > OffTaskThreshold)
        {
          var kind = gap > ShiftBoundary ? OffTaskKind.ShiftGap : OffTaskKind.Break;
          Then(new TimeOffTask(user, activity.LastTouchedAtUtc, touchedAtUtc, activity.LastFolderPath, currentFolder, kind, e.TemporalUser));
        }

        activity.LastTouchedAtUtc = touchedAtUtc;
        activity.LastFolderPath = currentFolder;
      }
      else
      {
        _userActivity[user] = new UserActivity
        {
          LastTouchedAtUtc = touchedAtUtc,
          LastFolderPath = currentFolder
        };
      }
    }
  }
}
