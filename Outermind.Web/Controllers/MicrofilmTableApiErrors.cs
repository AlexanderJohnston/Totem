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

    public static MicrofilmTableErrorEnvelope UnknownRoll(Id rollId) =>
      new(
        "UNKNOWN_ROLL",
        "Roll was not recognized.",
        new Dictionary<string, string> { ["rollId"] = rollId.ToString() });

    public static MicrofilmTableErrorEnvelope UnknownColumn(Id clientId, string columnId) =>
      new(
        "UNKNOWN_COLUMN",
        "Column was not recognized.",
        new Dictionary<string, string>
        {
          ["clientId"] = clientId.ToString(),
          ["columnId"] = columnId ?? ""
        });

    public static MicrofilmTableErrorEnvelope UnknownColumn(Id clientId, Id rollId, string columnId) =>
      new(
        "UNKNOWN_COLUMN",
        "Column was not recognized.",
        new Dictionary<string, string>
        {
          ["clientId"] = clientId.ToString(),
          ["rollId"] = rollId.ToString(),
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

    public static MicrofilmTableErrorEnvelope UnknownRow(Id clientId, Id rollId, string rowId) =>
      new(
        "UNKNOWN_ROW",
        "Row was not recognized.",
        new Dictionary<string, string>
        {
          ["clientId"] = clientId.ToString(),
          ["rollId"] = rollId.ToString(),
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

    public static MicrofilmTableErrorEnvelope UnknownProfile(string profileId) =>
      new(
        "UNKNOWN_PROFILE",
        "Profile was not recognized.",
        new Dictionary<string, string> { ["profileId"] = profileId ?? "" });

    public static MicrofilmTableErrorEnvelope InvalidProfileName(MicrofilmClientProfileNameRejected e) =>
      new(
        e.Code ?? "INVALID_PROFILE_NAME",
        e.Message ?? "Profile name is invalid.");

    public static MicrofilmTableErrorEnvelope DuplicateProfileName(MicrofilmClientProfileNameDuplicated e) =>
      new(
        "DUPLICATE_PROFILE_NAME",
        "Profile name already exists.",
        new Dictionary<string, string> { ["name"] = e.Name ?? "" });

    public static MicrofilmTableErrorEnvelope InvalidProfileColumns(MicrofilmClientProfileColumnsRejected e) =>
      new(
        e.Code ?? "INVALID_PROFILE_COLUMNS",
        e.Message ?? "Profile columns are invalid.",
        new Dictionary<string, string> { ["columnId"] = e.ColumnId ?? "" });

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

    public static MicrofilmTableErrorEnvelope InvalidRowKind(RollMicrofilmTableRowKindRejected e) =>
      new(
        "INVALID_ROW_KIND",
        "Row kind must be regular or custom.",
        new Dictionary<string, string>
        {
          ["clientId"] = e.ClientId.ToString(),
          ["rollId"] = e.RollId.ToString(),
          ["rowKind"] = e.RowKind ?? ""
        });

    public static MicrofilmTableErrorEnvelope RowKindMismatch(RollMicrofilmTableRowKindMismatch e) =>
      new(
        "ROW_KIND_MISMATCH",
        "Row route does not match the stored row kind.",
        new Dictionary<string, string>
        {
          ["clientId"] = e.ClientId.ToString(),
          ["rollId"] = e.RollId.ToString(),
          ["rowId"] = e.RowId ?? "",
          ["expectedRowKind"] = e.ExpectedRowKind ?? "",
          ["actualRowKind"] = e.ActualRowKind ?? ""
        });

    public static MicrofilmTableErrorEnvelope InvalidRequest(string message) =>
      new("INVALID_REQUEST", message);
  }
}
