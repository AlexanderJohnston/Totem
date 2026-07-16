using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Retains baseline roll fields for compatibility; API reads resolve the client catalog.
  /// </summary>
  public class RollMicrofilmColumnsQuery : Query
  {
    public Id RollId { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; } = new();

    static Id RouteFirst(RollCreated e) => e.Roll.RollId;

    void Given(RollCreated e)
    {
      RollId = e.Roll.RollId;
      Columns = MicrofilmDefaultColumns.RollScoped();
    }

  }
}
