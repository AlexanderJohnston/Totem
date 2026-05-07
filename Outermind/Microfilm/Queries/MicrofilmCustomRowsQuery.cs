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

    void Upsert(MicrofilmTableRow row)
    {
      CustomRows.RemoveAll(existing => existing.Id == row.Id);
      CustomRows.Add(row);
    }
  }
}
