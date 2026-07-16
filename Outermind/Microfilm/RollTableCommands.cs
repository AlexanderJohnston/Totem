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
    /// <summary>
    /// Immutable snapshot of the owning client's active catalog at dispatch time.
    /// Null is reserved for legacy callers, which fall back to historical roll state.
    /// </summary>
    public List<MicrofilmTableColumn> CatalogColumns { get; set; }

    public CreateRollMicrofilmRow(
      Id rollId,
      Id clientId,
      string rowId,
      string rowKind,
      Dictionary<string, MicrofilmCellValue> cells,
      MicrofilmAuditActorStamp actor,
      List<MicrofilmTableColumn> catalogColumns = null)
    {
      RollId = rollId;
      ClientId = clientId;
      RowId = rowId;
      RowKind = rowKind;
      Cells = cells ?? new Dictionary<string, MicrofilmCellValue>();
      Actor = actor;
      CatalogColumns = catalogColumns;
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
    /// <summary>See <see cref="CreateRollMicrofilmRow.CatalogColumns"/>.</summary>
    public List<MicrofilmTableColumn> CatalogColumns { get; set; }

    public UpdateRollMicrofilmRowCell(
      Id rollId,
      Id clientId,
      string rowId,
      string rowKind,
      string columnId,
      MicrofilmCellValue value,
      MicrofilmAuditActorStamp actor,
      List<MicrofilmTableColumn> catalogColumns = null)
    {
      RollId = rollId;
      ClientId = clientId;
      RowId = rowId;
      RowKind = rowKind;
      ColumnId = columnId;
      Value = value;
      Actor = actor;
      CatalogColumns = catalogColumns;
    }
  }
}
