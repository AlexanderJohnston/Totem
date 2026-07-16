using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks user-created custom table rows for a microfilm client.
  /// </summary>
  public class MicrofilmCustomRowsQuery : Query
  {
    public List<MicrofilmTableRow> CustomRows { get; set; } = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id Route(MicrofilmTableSeeded e) => e.ClientId;
    static Id Route(MicrofilmTableColumnsChanged e) => e.ClientId;
    static Id Route(MicrofilmCustomRowCreated e) => e.ClientId;
    static Id Route(MicrofilmCustomRowCellUpdated e) => e.ClientId;
    static Id Route(RollMicrofilmRowCreated e) => e.ClientId;
    static Id Route(RollMicrofilmRowCellChanged e) => e.ClientId;

    void Given(ClientCreated e)
    {
    }

    void Given(MicrofilmTableSeeded e)
    {
    }

    void Given(MicrofilmTableColumnsChanged e)
    {
      foreach(var row in CustomRows)
      {
        row.Cells = MicrofilmTableRules.ReconcileCells(e.Columns, row.Cells);
      }
    }

    void Given(MicrofilmCustomRowCreated e)
    {
      Upsert(e.Row.Clone(MicrofilmTableRowOrigins.Custom));
    }

    void Given(MicrofilmCustomRowCellUpdated e)
    {
      Upsert(e.Row.Clone(MicrofilmTableRowOrigins.Custom));
    }


    void Given(RollMicrofilmRowCreated e)
    {
      if(e.Row.Origin == MicrofilmTableRowOrigins.Custom)
      {
        Upsert(e.Row.Clone(MicrofilmTableRowOrigins.Custom));
      }
    }

    void Given(RollMicrofilmRowCellChanged e)
    {
      if(e.RowKind != MicrofilmTableRowOrigins.Custom)
      {
        return;
      }

      var row = CustomRows.FirstOrDefault(existing => existing.Id == e.RowId && existing.RollId == e.RollId.ToString());

      if(row == null)
      {
        return;
      }

      var updated = row.Clone(MicrofilmTableRowOrigins.Custom);
      updated.Cells[e.ColumnId] = e.Value?.Clone() ?? MicrofilmCellValue.Null();
      updated.CellAudits[e.ColumnId] = MicrofilmCellAudit.Tracked(e.When, e.Actor);
      Upsert(updated);
    }

    void Upsert(MicrofilmTableRow row)
    {
      CustomRows.RemoveAll(existing => existing.Id == row.Id && existing.RollId == row.RollId);
      CustomRows.Add(row);
    }
  }
}
