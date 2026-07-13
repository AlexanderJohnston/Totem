using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks one row addressed by rollId + rowId.
  /// </summary>
  public class RollMicrofilmRowQuery : Query
  {
    public MicrofilmTableRow Row { get; set; }

    static Id RouteFirst(RollMicrofilmRowCreated e) =>
      CreateId(e.RollId, e.Row.Id);

    static Id Route(RollMicrofilmRowCellChanged e) =>
      CreateId(e.RollId, e.RowId);

    void Given(RollMicrofilmRowCreated e)
    {
      Row = e.Row.Clone();
    }

    void Given(RollMicrofilmRowCellChanged e)
    {
      if(Row == null)
      {
        return;
      }

      Row.Cells[e.ColumnId] = e.Value?.Clone() ?? MicrofilmCellValue.Null();
      Row.CellAudits[e.ColumnId] = MicrofilmCellAudit.Tracked(e.When, e.Actor);
    }

    public static Id CreateId(Id rollId, string rowId) =>
      Id.FromMany(rollId, Id.From(rowId));
  }
}
