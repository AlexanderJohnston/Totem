using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Manages durable roll rows. The owning client catalog is supplied with current writes;
  /// this topic retains only historical roll columns as a legacy-command fallback.
  /// </summary>
  public class RollMicrofilmTableTopic : Topic
  {
    bool _rollRecognized;
    List<MicrofilmTableColumn> _legacyColumns = new();
    readonly Dictionary<string, MicrofilmTableRow> _rowsById = new();

    static Id RouteFirst(RollCreated e) => e.Roll.RollId;
    static Id RouteFirst(ReplaceRollMicrofilmTableColumns e) => e.RollId;
    static Id RouteFirst(CreateRollMicrofilmRow e) => e.RollId;
    static Id RouteFirst(UpdateRollMicrofilmRowCell e) => e.RollId;

    static Id Route(RollMicrofilmTableColumnsChanged e) => e.RollId;
    static Id Route(RollMicrofilmRowCreated e) => e.RollId;
    static Id Route(RollMicrofilmRowCellChanged e) => e.RollId;

    void Given(RollCreated e)
    {
      _rollRecognized = true;
      ApplyLegacyColumns(MicrofilmDefaultColumns.RollScoped());
    }

    void Given(RollMicrofilmTableColumnsChanged e)
    {
      // Historical events are retained only to support old commands without a catalog snapshot.
      // They must not reconcile or remove durable row values.
      ApplyLegacyColumns(e.Columns);
    }

    void Given(RollMicrofilmRowCreated e)
    {
      _rowsById[e.Row.Id] = e.Row.Clone();
    }

    void Given(RollMicrofilmRowCellChanged e)
    {
      if(!_rowsById.TryGetValue(e.RowId, out var row))
      {
        return;
      }

      var updated = row.Clone();
      updated.Cells[e.ColumnId] = e.Value?.Clone() ?? MicrofilmCellValue.Null();
      updated.CellAudits[e.ColumnId] = MicrofilmCellAudit.Tracked(e.When, e.Actor);
      _rowsById[e.RowId] = updated;
    }

    void When(ReplaceRollMicrofilmTableColumns command)
    {
      if(!EnsureRollRecognized(command.RollId) || !TryNormalizeColumns(command.ClientId, command.Columns, out var columns))
      {
        return;
      }

      // Deprecated compatibility command. The Web API routes catalog replacement to the client.
      Then(new RollMicrofilmTableColumnsChanged(
        command.RollId,
        command.ClientId,
        MicrofilmDefaultColumns.MergeRollCatalog(columns)));
    }

    void When(CreateRollMicrofilmRow command)
    {
      if(!EnsureRollRecognized(command.RollId))
      {
        return;
      }

      var rowKind = MicrofilmTableRowOrigins.Normalize(command.RowKind);

      if(!MicrofilmTableRowOrigins.IsSupported(rowKind))
      {
        Then(new RollMicrofilmTableRowKindRejected(command.RollId, command.ClientId, command.RowKind));
        return;
      }

      var rowId = string.IsNullOrWhiteSpace(command.RowId) ? Id.FromGuid().ToString() : command.RowId.Trim();

      if(_rowsById.ContainsKey(rowId))
      {
        Then(new MicrofilmTableRowConflict(command.ClientId, rowId, $"Row '{rowId}' already exists for roll '{command.RollId}'."));
        return;
      }

      if(!TryGetEffectiveColumns(command.ClientId, command.CatalogColumns, out var columns))
      {
        return;
      }

      if(!MicrofilmTableRules.TryNormalizeRow(
        rowId,
        rowKind,
        columns,
        command.Cells,
        out var row,
        out var code,
        out var message,
        out var columnId))
      {
        RejectRowInput(command.ClientId, rowId, code, message, columnId);
        return;
      }

      row.RollId = command.RollId.ToString();
      row.CellAudits = MicrofilmTableRules.ReconcileCellAudits(columns, row.CellAudits);

      Then(new RollMicrofilmRowCreated(command.RollId, command.ClientId, row, command.Actor));
    }

    void When(UpdateRollMicrofilmRowCell command)
    {
      if(!EnsureRollRecognized(command.RollId))
      {
        return;
      }

      if(string.IsNullOrWhiteSpace(command.RowId) || !_rowsById.TryGetValue(command.RowId, out var row))
      {
        Then(new MicrofilmTableRowNotRecognized(command.ClientId, command.RowId));
        return;
      }

      var rowKind = MicrofilmTableRowOrigins.Normalize(command.RowKind);

      if(!MicrofilmTableRowOrigins.IsSupported(rowKind))
      {
        Then(new RollMicrofilmTableRowKindRejected(command.RollId, command.ClientId, command.RowKind));
        return;
      }

      if(row.Origin != rowKind)
      {
        Then(new RollMicrofilmTableRowKindMismatch(command.RollId, command.ClientId, command.RowId, rowKind, row.Origin));
        return;
      }

      if(!TryGetEffectiveColumns(command.ClientId, command.CatalogColumns, out var columns))
      {
        return;
      }

      var column = columns.FirstOrDefault(c => c.Id == command.ColumnId);
      if(column == null)
      {
        Then(new MicrofilmTableColumnNotRecognized(command.ClientId, command.ColumnId));
        return;
      }

      if(!MicrofilmTableRules.TryNormalizeCell(column, command.Value, out var normalized, out var message))
      {
        Then(new MicrofilmTableCellValueRejected(command.ClientId, command.RowId, command.ColumnId, message));
        return;
      }

      Then(new RollMicrofilmRowCellChanged(command.RollId, command.ClientId, command.RowId, row.Origin, command.ColumnId, normalized, command.Actor));
    }

    bool EnsureRollRecognized(Id rollId)
    {
      if(_rollRecognized)
      {
        return true;
      }

      Then(new RollMicrofilmTableRollNotRecognized(rollId));
      return false;
    }

    bool TryGetEffectiveColumns(Id clientId, List<MicrofilmTableColumn> catalogSnapshot, out List<MicrofilmTableColumn> columns)
    {
      if(catalogSnapshot == null)
      {
        columns = _legacyColumns.Select(column => column.Clone()).ToList();
        return true;
      }

      if(!TryNormalizeColumns(clientId, catalogSnapshot, out var normalizedCatalog))
      {
        columns = null;
        return false;
      }

      columns = MicrofilmDefaultColumns.MergeRollCatalog(normalizedCatalog);
      return true;
    }

    bool TryNormalizeColumns(Id clientId, List<MicrofilmTableColumn> input, out List<MicrofilmTableColumn> columns)
    {
      if(MicrofilmTableRules.TryNormalizeColumns(input, out columns, out var code, out var message, out var columnId))
      {
        return true;
      }

      Then(new MicrofilmTableColumnSchemaRejected(clientId, code, message, columnId));
      return false;
    }

    void RejectRowInput(Id clientId, string rowId, string code, string message, string columnId)
    {
      if(code == "UNKNOWN_COLUMN")
      {
        Then(new MicrofilmTableColumnNotRecognized(clientId, columnId));
      }
      else
      {
        Then(new MicrofilmTableCellValueRejected(clientId, rowId, columnId, message));
      }
    }

    void ApplyLegacyColumns(List<MicrofilmTableColumn> columns)
    {
      _legacyColumns = (columns ?? new List<MicrofilmTableColumn>()).Select(column => column.Clone()).ToList();
    }
  }
}