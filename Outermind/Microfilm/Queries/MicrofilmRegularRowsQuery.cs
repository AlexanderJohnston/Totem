using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Queries
{
  /// <summary>
  /// Tracks backend-owned regular table rows for a microfilm client.
  /// </summary>
  public class MicrofilmRegularRowsQuery : Query
  {
    public List<MicrofilmTableRow> Rows { get; set; } = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id Route(MicrofilmTableSeeded e) => e.ClientId;
    static Id Route(MicrofilmTableColumnsChanged e) => e.ClientId;
    static Id Route(MicrofilmRegularRowCreated e) => e.ClientId;
    static Id Route(MicrofilmRegularRowCellUpdated e) => e.ClientId;
    static Id Route(RollMicrofilmTableColumnsChanged e) => e.ClientId;
    static Id Route(RollMicrofilmRowCreated e) => e.ClientId;
    static Id Route(RollMicrofilmRowCellChanged e) => e.ClientId;

    void Given(ClientCreated e)
    {
    }

    void Given(MicrofilmTableSeeded e)
    {
      Rows = e.RegularRows.Select(row => row.Clone(MicrofilmTableRowOrigins.Regular)).ToList();
    }

    void Given(MicrofilmTableColumnsChanged e)
    {
      foreach(var row in Rows)
      {
        row.Cells = MicrofilmTableRules.ReconcileCells(e.Columns, row.Cells);
      }
    }

    void Given(MicrofilmRegularRowCreated e)
    {
      Upsert(e.Row.Clone(MicrofilmTableRowOrigins.Regular));
    }

    void Given(MicrofilmRegularRowCellUpdated e)
    {
      Upsert(e.Row.Clone(MicrofilmTableRowOrigins.Regular));
    }

    void Given(RollMicrofilmTableColumnsChanged e)
    {
      foreach(var row in Rows)
      {
        row.Cells = MicrofilmTableRules.ReconcileCells(e.Columns, row.Cells);
        row.CellAudits = MicrofilmTableRules.ReconcileCellAudits(e.Columns, row.CellAudits);
      }
    }

    void Given(RollMicrofilmRowCreated e)
    {
      if(e.Row.Origin == MicrofilmTableRowOrigins.Regular)
      {
        Upsert(e.Row.Clone(MicrofilmTableRowOrigins.Regular));
      }
    }

    void Given(RollMicrofilmRowCellChanged e)
    {
      if(e.RowKind != MicrofilmTableRowOrigins.Regular)
      {
        return;
      }

      var row = Rows.FirstOrDefault(existing => existing.Id == e.RowId && existing.RollId == e.RollId.ToString());

      if(row == null)
      {
        return;
      }

      var updated = row.Clone(MicrofilmTableRowOrigins.Regular);
      updated.Cells[e.ColumnId] = e.Value?.Clone() ?? MicrofilmCellValue.Null();
      updated.CellAudits[e.ColumnId] = MicrofilmCellAudit.Tracked(e.When, e.Actor);
      Upsert(updated);
    }

    void Upsert(MicrofilmTableRow row)
    {
      Rows.RemoveAll(existing => existing.Id == row.Id && existing.RollId == row.RollId);
      Rows.Add(row);
    }
  }
}
