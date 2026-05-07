using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public class MicrofilmTableSeeded : Event
  {
    public Id ClientId { get; set; }
    public string SeedId { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; }
    public List<MicrofilmTableRow> RegularRows { get; set; }

    public MicrofilmTableSeeded(Id clientId, string seedId, List<MicrofilmTableColumn> columns, List<MicrofilmTableRow> regularRows)
    {
      ClientId = clientId;
      SeedId = seedId;
      Columns = columns ?? new List<MicrofilmTableColumn>();
      RegularRows = regularRows ?? new List<MicrofilmTableRow>();
    }
  }

  public class MicrofilmTableSeedAlreadyApplied : Event
  {
    public Id ClientId { get; set; }
    public string SeedId { get; set; }

    public MicrofilmTableSeedAlreadyApplied(Id clientId, string seedId)
    {
      ClientId = clientId;
      SeedId = seedId;
    }
  }

  public class MicrofilmTableColumnsChanged : Event
  {
    public Id ClientId { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; }

    public MicrofilmTableColumnsChanged(Id clientId, List<MicrofilmTableColumn> columns)
    {
      ClientId = clientId;
      Columns = columns ?? new List<MicrofilmTableColumn>();
    }
  }

  public class MicrofilmRegularRowCreated : Event
  {
    public Id ClientId { get; set; }
    public MicrofilmTableRow Row { get; set; }

    public MicrofilmRegularRowCreated(Id clientId, MicrofilmTableRow row)
    {
      ClientId = clientId;
      Row = row;
    }
  }

  public class MicrofilmRegularRowCellUpdated : Event
  {
    public Id ClientId { get; set; }
    public MicrofilmTableRow Row { get; set; }

    public MicrofilmRegularRowCellUpdated(Id clientId, MicrofilmTableRow row)
    {
      ClientId = clientId;
      Row = row;
    }
  }

  public class MicrofilmCustomRowCreated : Event
  {
    public Id ClientId { get; set; }
    public MicrofilmTableRow Row { get; set; }

    public MicrofilmCustomRowCreated(Id clientId, MicrofilmTableRow row)
    {
      ClientId = clientId;
      Row = row;
    }
  }

  public class MicrofilmCustomRowCellUpdated : Event
  {
    public Id ClientId { get; set; }
    public MicrofilmTableRow Row { get; set; }

    public MicrofilmCustomRowCellUpdated(Id clientId, MicrofilmTableRow row)
    {
      ClientId = clientId;
      Row = row;
    }
  }

  public class MicrofilmTableClientNotRecognized : Event
  {
    public Id ClientId { get; set; }

    public MicrofilmTableClientNotRecognized(Id clientId)
    {
      ClientId = clientId;
    }
  }

  public class MicrofilmTableColumnNotRecognized : Event
  {
    public Id ClientId { get; set; }
    public string ColumnId { get; set; }

    public MicrofilmTableColumnNotRecognized(Id clientId, string columnId)
    {
      ClientId = clientId;
      ColumnId = columnId;
    }
  }

  public class MicrofilmTableRowNotRecognized : Event
  {
    public Id ClientId { get; set; }
    public string RowId { get; set; }

    public MicrofilmTableRowNotRecognized(Id clientId, string rowId)
    {
      ClientId = clientId;
      RowId = rowId;
    }
  }

  public class MicrofilmTableColumnSchemaRejected : Event
  {
    public Id ClientId { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string ColumnId { get; set; }

    public MicrofilmTableColumnSchemaRejected(Id clientId, string code, string message, string columnId = null)
    {
      ClientId = clientId;
      Code = code;
      Message = message;
      ColumnId = columnId;
    }
  }

  public class MicrofilmTableCellValueRejected : Event
  {
    public Id ClientId { get; set; }
    public string RowId { get; set; }
    public string ColumnId { get; set; }
    public string Message { get; set; }

    public MicrofilmTableCellValueRejected(Id clientId, string rowId, string columnId, string message)
    {
      ClientId = clientId;
      RowId = rowId;
      ColumnId = columnId;
      Message = message;
    }
  }

  public class MicrofilmTableRowConflict : Event
  {
    public Id ClientId { get; set; }
    public string RowId { get; set; }
    public string Message { get; set; }

    public MicrofilmTableRowConflict(Id clientId, string rowId, string message)
    {
      ClientId = clientId;
      RowId = rowId;
      Message = message;
    }
  }
}
