using System.Collections.Generic;
using System.Linq;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm.Topics
{
  /// <summary>
  /// Manages the generic editable table owned by each microfilm client.
  /// </summary>
  public class MicrofilmTableTopic : Topic
  {
    bool _clientRecognized;
    readonly HashSet<string> _seedIds = new();
    List<MicrofilmTableColumn> _columns = new();
    readonly Dictionary<string, MicrofilmTableRow> _regularRowsById = new();
    readonly Dictionary<string, MicrofilmTableRow> _customRowsById = new();

    static Id RouteFirst(ClientCreated e) => e.Client.ClientId;
    static Id Route(ClientReassigned e) => e.Client.ClientId;

    static Id RouteFirst(SeedMicrofilmTable e) => e.ClientId;
    static Id RouteFirst(ReplaceMicrofilmTableColumns e) => e.ClientId;
    static Id RouteFirst(CreateMicrofilmRegularRow e) => e.ClientId;
    static Id RouteFirst(UpdateMicrofilmRegularRowCell e) => e.ClientId;
    static Id RouteFirst(CreateMicrofilmCustomRow e) => e.ClientId;
    static Id RouteFirst(UpdateMicrofilmCustomRowCell e) => e.ClientId;

    static Id Route(MicrofilmTableSeeded e) => e.ClientId;
    static Id Route(MicrofilmTableColumnsChanged e) => e.ClientId;
    static Id Route(MicrofilmRegularRowCreated e) => e.ClientId;
    static Id Route(MicrofilmRegularRowCellUpdated e) => e.ClientId;
    static Id Route(MicrofilmCustomRowCreated e) => e.ClientId;
    static Id Route(MicrofilmCustomRowCellUpdated e) => e.ClientId;

    void Given(ClientCreated e)
    {
      _clientRecognized = true;
    }

    void Given(ClientReassigned e)
    {
      _clientRecognized = true;
    }

    void Given(MicrofilmTableSeeded e)
    {
      _seedIds.Add(e.SeedId);
      ApplyColumns(e.Columns);

      foreach(var row in e.RegularRows ?? new List<MicrofilmTableRow>())
      {
        _regularRowsById[row.Id] = row.Clone(MicrofilmTableRowOrigins.Regular);
      }
    }

    void Given(MicrofilmTableColumnsChanged e)
    {
      ApplyColumns(e.Columns);
    }

    void Given(MicrofilmRegularRowCreated e)
    {
      _regularRowsById[e.Row.Id] = e.Row.Clone(MicrofilmTableRowOrigins.Regular);
    }

    void Given(MicrofilmRegularRowCellUpdated e)
    {
      _regularRowsById[e.Row.Id] = e.Row.Clone(MicrofilmTableRowOrigins.Regular);
    }

    void Given(MicrofilmCustomRowCreated e)
    {
      _customRowsById[e.Row.Id] = e.Row.Clone(MicrofilmTableRowOrigins.Custom);
    }

    void Given(MicrofilmCustomRowCellUpdated e)
    {
      _customRowsById[e.Row.Id] = e.Row.Clone(MicrofilmTableRowOrigins.Custom);
    }

    void When(SeedMicrofilmTable command)
    {
      if(!EnsureClientRecognized(command.ClientId))
      {
        return;
      }

      var seedId = string.IsNullOrWhiteSpace(command.SeedId)
        ? command.ClientId.ToString()
        : command.SeedId.Trim();

      if(_seedIds.Contains(seedId) || _columns.Count > 0 || _regularRowsById.Count > 0 || _customRowsById.Count > 0)
      {
        Then(new MicrofilmTableSeedAlreadyApplied(command.ClientId, seedId));
        return;
      }

      if(!TryNormalizeColumns(command.ClientId, command.Columns, out var columns))
      {
        return;
      }

      if(!TryNormalizeRows(command.ClientId, command.RegularRows, columns, MicrofilmTableRowOrigins.Regular, out var rows))
      {
        return;
      }

      Then(new MicrofilmTableSeeded(command.ClientId, seedId, columns, rows));
    }

    void When(ReplaceMicrofilmTableColumns command)
    {
      if(!EnsureClientRecognized(command.ClientId) || !TryNormalizeColumns(command.ClientId, command.Columns, out var columns))
      {
        return;
      }

      Then(new MicrofilmTableColumnsChanged(command.ClientId, columns));
    }

    void When(CreateMicrofilmRegularRow command)
    {
      if(!EnsureClientRecognized(command.ClientId))
      {
        return;
      }

      var rowId = string.IsNullOrWhiteSpace(command.RowId) ? Id.FromGuid().ToString() : command.RowId.Trim();

      if(_regularRowsById.ContainsKey(rowId) || _customRowsById.ContainsKey(rowId))
      {
        Then(new MicrofilmTableRowConflict(command.ClientId, rowId, $"Row '{rowId}' already exists."));
        return;
      }

      if(!MicrofilmTableRules.TryNormalizeOptimisticRow(
        rowId,
        MicrofilmTableRowOrigins.Regular,
        command.Cells,
        null,
        out var row,
        out var code,
        out var message,
        out var columnId))
      {
        RejectRowInput(command.ClientId, rowId, code, message, columnId);
        return;
      }

      Then(new MicrofilmRegularRowCreated(command.ClientId, row));
    }

    void When(UpdateMicrofilmRegularRowCell command)
    {
      UpdateCell(
        command.ClientId,
        command.RowId,
        command.ColumnId,
        command.Value,
        _regularRowsById,
        row => new MicrofilmRegularRowCellUpdated(command.ClientId, row));
    }

    void When(CreateMicrofilmCustomRow command)
    {
      if(!EnsureClientRecognized(command.ClientId))
      {
        return;
      }

      var rowId = Id.FromGuid().ToString();

      if(!MicrofilmTableRules.TryNormalizeOptimisticRow(
        rowId,
        MicrofilmTableRowOrigins.Custom,
        command.Cells,
        null,
        out var row,
        out var code,
        out var message,
        out var columnId))
      {
        RejectRowInput(command.ClientId, rowId, code, message, columnId);
        return;
      }

      Then(new MicrofilmCustomRowCreated(command.ClientId, row));
    }

    void When(UpdateMicrofilmCustomRowCell command)
    {
      UpdateCell(
        command.ClientId,
        command.RowId,
        command.ColumnId,
        command.Value,
        _customRowsById,
        row => new MicrofilmCustomRowCellUpdated(command.ClientId, row));
    }

    bool EnsureClientRecognized(Id clientId)
    {
      if(_clientRecognized)
      {
        return true;
      }

      Then(new MicrofilmTableClientNotRecognized(clientId));
      return false;
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

    bool TryNormalizeRows(
      Id clientId,
      List<MicrofilmTableRow> input,
      List<MicrofilmTableColumn> columns,
      string origin,
      out List<MicrofilmTableRow> rows)
    {
      rows = new List<MicrofilmTableRow>();
      var rowIds = new HashSet<string>();

      foreach(var source in input ?? new List<MicrofilmTableRow>())
      {
        var rowId = string.IsNullOrWhiteSpace(source?.Id) ? Id.FromGuid().ToString() : source.Id.Trim();

        if(!rowIds.Add(rowId))
        {
          Then(new MicrofilmTableRowConflict(clientId, rowId, $"Row '{rowId}' is duplicated."));
          return false;
        }

        if(!MicrofilmTableRules.TryNormalizeRow(
          rowId,
          origin,
          columns,
          source?.Cells,
          out var row,
          out var code,
          out var message,
          out var columnId))
        {
          RejectRowInput(clientId, rowId, code, message, columnId);
          return false;
        }

        rows.Add(row);
      }

      return true;
    }

    void UpdateCell(
      Id clientId,
      string rowId,
      string columnId,
      MicrofilmCellValue value,
      Dictionary<string, MicrofilmTableRow> rowsById,
      System.Func<MicrofilmTableRow, Event> createEvent)
    {
      if(!EnsureClientRecognized(clientId))
      {
        return;
      }

      if(string.IsNullOrWhiteSpace(rowId) || !rowsById.TryGetValue(rowId, out var row))
      {
        Then(new MicrofilmTableRowNotRecognized(clientId, rowId));
        return;
      }

      if(!MicrofilmTableRules.TryNormalizeColumnId(columnId, out var normalizedColumnId, out var message)
        || !MicrofilmTableRules.TryNormalizeCellValue(normalizedColumnId, value, out var normalized, out message))
      {
        Then(new MicrofilmTableCellValueRejected(clientId, rowId, normalizedColumnId ?? columnId, message));
        return;
      }

      var updated = row.Clone();
      updated.Cells[normalizedColumnId] = normalized;

      Then(createEvent(updated));
    }

    void RejectRowInput(Id clientId, string rowId, string code, string message, string columnId)
    {
      Then(new MicrofilmTableCellValueRejected(clientId, rowId, columnId, message));
    }

    void ApplyColumns(List<MicrofilmTableColumn> columns)
    {
      _columns = (columns ?? new List<MicrofilmTableColumn>()).Select(column => column.Clone()).ToList();
    }
  }
}
