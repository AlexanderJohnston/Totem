using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public class SeedMicrofilmTable : Command
  {
    public Id ClientId { get; set; }
    public string SeedId { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; }
    public List<MicrofilmTableRow> RegularRows { get; set; }

    public SeedMicrofilmTable(Id clientId, string seedId, List<MicrofilmTableColumn> columns, List<MicrofilmTableRow> regularRows)
    {
      ClientId = clientId;
      SeedId = seedId;
      Columns = columns ?? new List<MicrofilmTableColumn>();
      RegularRows = regularRows ?? new List<MicrofilmTableRow>();
    }
  }

  public class ReplaceMicrofilmTableColumns : Command
  {
    public Id ClientId { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; }

    public ReplaceMicrofilmTableColumns(Id clientId, List<MicrofilmTableColumn> columns)
    {
      ClientId = clientId;
      Columns = columns ?? new List<MicrofilmTableColumn>();
    }
  }

  public class CreateMicrofilmRegularRow : Command
  {
    public Id ClientId { get; set; }
    public string RowId { get; set; }
    public Dictionary<string, MicrofilmCellValue> Cells { get; set; }

    public CreateMicrofilmRegularRow(Id clientId, string rowId, Dictionary<string, MicrofilmCellValue> cells)
    {
      ClientId = clientId;
      RowId = rowId;
      Cells = cells ?? new Dictionary<string, MicrofilmCellValue>();
    }
  }

  public class UpdateMicrofilmRegularRowCell : Command
  {
    public Id ClientId { get; set; }
    public string RowId { get; set; }
    public string ColumnId { get; set; }
    public MicrofilmCellValue Value { get; set; }

    public UpdateMicrofilmRegularRowCell(Id clientId, string rowId, string columnId, MicrofilmCellValue value)
    {
      ClientId = clientId;
      RowId = rowId;
      ColumnId = columnId;
      Value = value;
    }
  }

  public class CreateMicrofilmCustomRow : Command
  {
    public Id ClientId { get; set; }
    public Dictionary<string, MicrofilmCellValue> Cells { get; set; }

    public CreateMicrofilmCustomRow(Id clientId, Dictionary<string, MicrofilmCellValue> cells)
    {
      ClientId = clientId;
      Cells = cells ?? new Dictionary<string, MicrofilmCellValue>();
    }
  }

  public class UpdateMicrofilmCustomRowCell : Command
  {
    public Id ClientId { get; set; }
    public string RowId { get; set; }
    public string ColumnId { get; set; }
    public MicrofilmCellValue Value { get; set; }

    public UpdateMicrofilmCustomRowCell(Id clientId, string rowId, string columnId, MicrofilmCellValue value)
    {
      ClientId = clientId;
      RowId = rowId;
      ColumnId = columnId;
      Value = value;
    }
  }
}
