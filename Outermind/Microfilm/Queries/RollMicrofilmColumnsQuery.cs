using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks effective column definitions for one roll-scoped table.
  /// </summary>
  public class RollMicrofilmColumnsQuery : Query
  {
    public Id RollId { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; } = new();

    static Id RouteFirst(RollCreated e) => e.Roll.RollId;
    static Id Route(RollMicrofilmTableColumnsChanged e) => e.RollId;

    void Given(RollCreated e)
    {
      RollId = e.Roll.RollId;
      Columns = MicrofilmDefaultColumns.RollScoped();
    }

    void Given(RollMicrofilmTableColumnsChanged e)
    {
      RollId = e.RollId;
      Columns = e.Columns.Select(column => column.Clone()).ToList();
    }
  }
}
