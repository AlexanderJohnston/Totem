using System.Collections.Generic;
using Outermind.Microfilm;
using Totem;

namespace Outermind.Controllers
{
  public static class MicrofilmTableApiErrors
  {
    public static MicrofilmTableErrorEnvelope UnknownServer(Id serverId) =>
      new(
        "UNKNOWN_SERVER",
        "Server was not recognized.",
        new Dictionary<string, string> { ["serverId"] = serverId.ToString() });

    public static MicrofilmTableErrorEnvelope UnknownClient(Id clientId) =>
      new(
        "UNKNOWN_CLIENT",
        "Client was not recognized.",
        new Dictionary<string, string> { ["clientId"] = clientId.ToString() });

    public static MicrofilmTableErrorEnvelope UnknownColumn(Id clientId, string columnId) =>
      new(
        "UNKNOWN_COLUMN",
        "Column was not recognized.",
        new Dictionary<string, string>
        {
          ["clientId"] = clientId.ToString(),
          ["columnId"] = columnId ?? ""
        });

    public static MicrofilmTableErrorEnvelope UnknownRow(Id clientId, string rowId) =>
      new(
        "UNKNOWN_ROW",
        "Row was not recognized.",
        new Dictionary<string, string>
        {
          ["clientId"] = clientId.ToString(),
          ["rowId"] = rowId ?? ""
        });

    public static MicrofilmTableErrorEnvelope InvalidColumns(MicrofilmTableColumnSchemaRejected e) =>
      new(
        e.Code ?? "INVALID_COLUMNS",
        e.Message ?? "Column schema is invalid.",
        new Dictionary<string, string>
        {
          ["clientId"] = e.ClientId.ToString(),
          ["columnId"] = e.ColumnId ?? ""
        });

    public static MicrofilmTableErrorEnvelope InvalidCell(MicrofilmTableCellValueRejected e) =>
      new(
        "INVALID_CELL_VALUE",
        e.Message ?? "Cell value is invalid.",
        new Dictionary<string, string>
        {
          ["clientId"] = e.ClientId.ToString(),
          ["rowId"] = e.RowId ?? "",
          ["columnId"] = e.ColumnId ?? ""
        });

    public static MicrofilmTableErrorEnvelope RowConflict(MicrofilmTableRowConflict e) =>
      new(
        "ROW_CONFLICT",
        e.Message ?? "Row conflicts with existing table state.",
        new Dictionary<string, string>
        {
          ["clientId"] = e.ClientId.ToString(),
          ["rowId"] = e.RowId ?? ""
        });

    public static MicrofilmTableErrorEnvelope InvalidRequest(string message) =>
      new("INVALID_REQUEST", message);
  }
}
