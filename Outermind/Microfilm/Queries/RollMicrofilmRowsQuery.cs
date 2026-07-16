using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks rows for one roll-scoped table.
  /// </summary>
  public class RollMicrofilmRowsQuery : Query
  {
    public Id RollId { get; set; }
    public List<MicrofilmTableRow> Rows { get; set; } = new();

    static Id RouteFirst(RollCreated e) => e.Roll.RollId;
    static Id Route(RollMicrofilmRowCreated e) => e.RollId;
    static Id Route(RollMicrofilmRowCellChanged e) => e.RollId;

    void Given(RollCreated e)
    {
      RollId = e.Roll.RollId;
    }


    void Given(RollMicrofilmRowCreated e)
    {
      RollId = e.RollId;
      Upsert(e.Row.Clone());
    }

    void Given(RollMicrofilmRowCellChanged e)
    {
      RollId = e.RollId;
      var row = Rows.FirstOrDefault(existing => existing.Id == e.RowId);

      if(row == null)
      {
        return;
      }

      var updated = row.Clone();
      updated.Cells[e.ColumnId] = e.Value?.Clone() ?? MicrofilmCellValue.Null();
      updated.CellAudits[e.ColumnId] = MicrofilmCellAudit.Tracked(e.When, e.Actor);
      Upsert(updated);
    }

    void Upsert(MicrofilmTableRow row)
    {
      Rows.RemoveAll(existing => existing.Id == row.Id);
      Rows.Add(row);
    }
  }
}
