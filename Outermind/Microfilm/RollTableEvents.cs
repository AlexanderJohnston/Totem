using System.Collections.Generic;
using Totem;
using Totem.Timeline;

namespace Outermind.Microfilm
{
  public class RollMicrofilmTableColumnsChanged : Event
  {
    public Id RollId { get; set; }
    public Id ClientId { get; set; }
    public List<MicrofilmTableColumn> Columns { get; set; }

    public RollMicrofilmTableColumnsChanged(Id rollId, Id clientId, List<MicrofilmTableColumn> columns)
    {
      RollId = rollId;
      ClientId = clientId;
      Columns = columns ?? new List<MicrofilmTableColumn>();
    }
  }

  public class RollMicrofilmRowCreated : Event
  {
    public Id RollId { get; set; }
    public Id ClientId { get; set; }
    public MicrofilmTableRow Row { get; set; }
    public MicrofilmAuditActorStamp Actor { get; set; }

    public RollMicrofilmRowCreated(Id rollId, Id clientId, MicrofilmTableRow row, MicrofilmAuditActorStamp actor)
    {
      RollId = rollId;
      ClientId = clientId;
      Row = row;
      Actor = actor;
    }
  }

  public class RollMicrofilmRowCellChanged : Event
  {
    public Id RollId { get; set; }
    public Id ClientId { get; set; }
    public string RowId { get; set; }
    public string RowKind { get; set; }
    public string ColumnId { get; set; }
    public MicrofilmCellValue Value { get; set; }
    public MicrofilmAuditActorStamp Actor { get; set; }

    public RollMicrofilmRowCellChanged(
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

  public class RollMicrofilmTableRollNotRecognized : Event
  {
    public Id RollId { get; set; }

    public RollMicrofilmTableRollNotRecognized(Id rollId)
    {
      RollId = rollId;
    }
  }

  public class RollMicrofilmTableRowKindRejected : Event
  {
    public Id RollId { get; set; }
    public Id ClientId { get; set; }
    public string RowKind { get; set; }

    public RollMicrofilmTableRowKindRejected(Id rollId, Id clientId, string rowKind)
    {
      RollId = rollId;
      ClientId = clientId;
      RowKind = rowKind;
    }
  }

  public class RollMicrofilmTableRowKindMismatch : Event
  {
    public Id RollId { get; set; }
    public Id ClientId { get; set; }
    public string RowId { get; set; }
    public string ExpectedRowKind { get; set; }
    public string ActualRowKind { get; set; }

    public RollMicrofilmTableRowKindMismatch(
      Id rollId,
      Id clientId,
      string rowId,
      string expectedRowKind,
      string actualRowKind)
    {
      RollId = rollId;
      ClientId = clientId;
      RowId = rowId;
      ExpectedRowKind = expectedRowKind;
      ActualRowKind = actualRowKind;
    }
  }
}
