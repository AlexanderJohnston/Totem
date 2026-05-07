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

    void Upsert(MicrofilmTableRow row)
    {
      Rows.RemoveAll(existing => existing.Id == row.Id);
      Rows.Add(row);
    }
  }
}
