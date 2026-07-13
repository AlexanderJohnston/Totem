using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public class ReplaceRollMicrofilmTableColumns : Command
  {
    public Id RollId { get; set; }
    public Id ClientId { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; }

    public ReplaceRollMicrofilmTableColumns(Id rollId, Id clientId, List<MicrofilmTableColumn> columns)
    {
      RollId = rollId;
      ClientId = clientId;
      Columns = columns ?? new List<MicrofilmTableColumn>();
    }
  }

  public class CreateRollMicrofilmRow : Command
  {
    public Id RollId { get; set; }
    public Id ClientId { get; set; }
    public string RowId { get; set; }
    public string RowKind { get; set; }
    public Dictionary<string, MicrofilmCellValue> Cells { get; set; }
    public MicrofilmAuditActorStamp Actor { get; set; }

    public CreateRollMicrofilmRow(
      Id rollId,
      Id clientId,
      string rowId,
      string rowKind,
      Dictionary<string, MicrofilmCellValue> cells,
      MicrofilmAuditActorStamp actor)
    {
      RollId = rollId;
      ClientId = clientId;
      RowId = rowId;
      RowKind = rowKind;
      Cells = cells ?? new Dictionary<string, MicrofilmCellValue>();
      Actor = actor;
    }
  }

  public class UpdateRollMicrofilmRowCell : Command
  {
    public Id RollId { get; set; }
    public Id ClientId { get; set; }
    public string RowId { get; set; }
    public string RowKind { get; set; }
    public string ColumnId { get; set; }
    public MicrofilmCellValue Value { get; set; }
    public MicrofilmAuditActorStamp Actor { get; set; }

    public UpdateRollMicrofilmRowCell(
      Id rollId,
      Id clientId,
      string rowId,
      string rowKind,
      string columnId,
      MicrofilmCellValue value,
      MicrofilmAuditActorStamp actor)
    {
      RollId = rollId;
      ClientId = clientId;
      RowId = rowId;
      RowKind = rowKind;
      ColumnId = columnId;
      Value = value;
      Actor = actor;
    }
  }
}
