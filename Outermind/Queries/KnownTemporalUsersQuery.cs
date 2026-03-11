using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Queries
{
  public class KnownTemporalUsersQuery : Query
  {
    public HashSet<string> TemporalUserIds { get; set; } = new();

    static Id RouteFirst(TimeOffTask e) => e.UserId;

    void Given(TimeOffTask e)
    {
      TemporalUserIds.Add(e.TemporalUser.ToString());
    }
  }
}
