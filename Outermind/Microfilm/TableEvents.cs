using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
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
