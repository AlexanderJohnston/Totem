using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Outermind.Microfilm.Topics;
using Totem;
using Totem.App.Tests;
using Totem.Timeline.Area;
using Xunit;

namespace Quantum.Tests
{
  public class RollMicrofilmOptimisticFieldsRegressionTests : TopicTests<RollMicrofilmTableTopic>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");
    static readonly Id RollId = Id.From("00000000-0000-0000-0000-000000000401");

    [Fact]
    public async Task CanonicalRollCreateAndUpdate_AcceptUnknownArbitraryScalarFields()
    {
      await Append(new RollCreated(new KnownRoll("Roll A", RollId, Id.Unassigned)));
      await Append(new CreateRollMicrofilmRow(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue>
        {
          ["note"] = MicrofilmCellValue.FromText("New"),
          ["frameCount"] = MicrofilmCellValue.FromNumber(12),
          ["reviewed"] = MicrofilmCellValue.FromCheckbox(true),
          ["retiredField"] = null
        }, Actor()));
      var created = await Expect<RollMicrofilmRowCreated>();

      await Append(new UpdateRollMicrofilmRowCell(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        " unknownAfterCreate ", MicrofilmCellValue.FromText("accepted"), Actor()));

      var changed = await Expect<RollMicrofilmRowCellChanged>();
      Assert.Equal("New", created.Row.Cells["note"].Text);
      Assert.Equal(12, created.Row.Cells["frameCount"].Number);
      Assert.True(created.Row.Cells["reviewed"].Checkbox);
      Assert.Equal(MicrofilmCellValueKind.Null, created.Row.Cells["retiredField"].Kind);
      Assert.Equal(MicrofilmCellValueKind.Null, created.Row.Cells["boxName"].Kind);
      Assert.Equal(MicrofilmCellValueKind.Null, created.Row.Cells["rollName"].Kind);
      Assert.Equal("unknownAfterCreate", changed.ColumnId);
      Assert.Equal("accepted", changed.Value.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CanonicalRollUpdate_RejectsEmptyFieldIds(string columnId)
    {
      await Append(new RollCreated(new KnownRoll("Roll A", RollId, Id.Unassigned)));
      await Append(new CreateRollMicrofilmRow(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue>(), Actor()));
      await Expect<RollMicrofilmRowCreated>();

      await Append(new UpdateRollMicrofilmRowCell(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        columnId, MicrofilmCellValue.FromText("rejected"), Actor()));

      Assert.Equal(string.Empty, (await Expect<MicrofilmTableCellValueRejected>()).ColumnId);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task CanonicalRollCreate_RejectsObjectAndArrayCellRepresentations(string json)
    {
      await Append(new RollCreated(new KnownRoll("Roll A", RollId, Id.Unassigned)));
      var value = JsonSerializer.Deserialize<MicrofilmCellValue>(json);
      Assert.Equal(MicrofilmCellValueKind.Unsupported, value.Kind);

      await Append(new CreateRollMicrofilmRow(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue> { ["metadata"] = value }, Actor()));

      Assert.Equal("metadata", (await Expect<MicrofilmTableCellValueRejected>()).ColumnId);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task CanonicalRollUpdate_RejectsObjectAndArrayCellRepresentations(string json)
    {
      await Append(new RollCreated(new KnownRoll("Roll A", RollId, Id.Unassigned)));
      await Append(new CreateRollMicrofilmRow(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue>(), Actor()));
      await Expect<RollMicrofilmRowCreated>();
      var value = JsonSerializer.Deserialize<MicrofilmCellValue>(json);
      Assert.Equal(MicrofilmCellValueKind.Unsupported, value.Kind);

      await Append(new UpdateRollMicrofilmRowCell(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        "metadata", value, Actor()));

      Assert.Equal("metadata", (await Expect<MicrofilmTableCellValueRejected>()).ColumnId);
    }

    static MicrofilmAuditActorStamp Actor() =>
      new("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie");
  }


  public class RollMicrofilmOptimisticFieldRowsQueryTests : QueryTests<RollMicrofilmRowsQuery>
  {
    [Fact]
    public async Task ListRead_ReturnsDurableCellsAndAudits()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");
      var rollId = Id.From("00000000-0000-0000-0000-000000000401");
      var actor = new MicrofilmAuditActorStamp("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie");
      await Append(new RollCreated(new KnownRoll("Roll A", rollId, Id.Unassigned)));
      await Append(new RollMicrofilmRowCreated(rollId, clientId,
        new MicrofilmTableRow("row-1", rollId.ToString(), MicrofilmTableRowOrigins.Regular,
          new Dictionary<string, MicrofilmCellValue> { ["sharedField"] = MicrofilmCellValue.FromText("preserved") }), actor));
      await Append(new RollMicrofilmRowCellChanged(rollId, clientId, "row-1", MicrofilmTableRowOrigins.Regular,
        "sharedField", MicrofilmCellValue.FromText("durable"), actor));
      var row = Assert.Single((await GetQuery(rollId)).Rows);
      Assert.Equal("durable", row.Cells["sharedField"].Text);
      Assert.Equal(MicrofilmCellAuditStates.Tracked, row.CellAudits["sharedField"].State);
      Assert.Equal("AJOHNSTON", row.CellAudits["sharedField"].LastChangedBy.ProcessUserId);
    }

    [Fact]
    public void RollRowsQuery_DoesNotObserveHistoricalColumnEvents()
    {
      var area = AreaMap.From(typeof(RollMicrofilmRowsQuery).Assembly.GetTypes());
      QueryType queryType = area.Queries.Get<RollMicrofilmRowsQuery>();

      Assert.False(queryType.Observations.Contains(typeof(RollMicrofilmTableColumnsChanged)));
    }
  }

  public class RollMicrofilmOptimisticFieldTableQueryTests : QueryTests<RollMicrofilmTableQuery>
  {
    [Fact]
    public async Task TableRead_ReturnsTheSameDurableValueAndAudit()
    {
      var rollId = Id.From("00000000-0000-0000-0000-000000000401");
      var actor = new MicrofilmAuditActorStamp("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie");
      await Append(new RollCreated(new KnownRoll("Roll A", rollId, Id.Unassigned)));
      await Append(new RollMicrofilmRowCreated(rollId, Id.Unassigned,
        new MicrofilmTableRow("row-1", rollId.ToString(), MicrofilmTableRowOrigins.Regular,
          new Dictionary<string, MicrofilmCellValue> { ["sharedField"] = MicrofilmCellValue.FromText("preserved") }), actor));
      await Append(new RollMicrofilmRowCellChanged(rollId, Id.Unassigned, "row-1", MicrofilmTableRowOrigins.Regular,
        "sharedField", MicrofilmCellValue.FromText("durable"), actor));

      var row = Assert.Single((await GetQuery(rollId)).Rows);
      Assert.Equal("durable", row.Cells["sharedField"].Text);
      Assert.Equal("AJOHNSTON", row.CellAudits["sharedField"].LastChangedBy.ProcessUserId);
    }
  }

  public class RollMicrofilmOptimisticFieldRowQueryTests : QueryTests<RollMicrofilmRowQuery>
  {
    [Fact]
    public async Task SingleRowRead_ReturnsTheSameDurableValueAndAudit()
    {
      var rollId = Id.From("00000000-0000-0000-0000-000000000401");
      var actor = new MicrofilmAuditActorStamp("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie");
      await Append(new RollMicrofilmRowCreated(rollId, Id.Unassigned,
        new MicrofilmTableRow("row-1", rollId.ToString(), MicrofilmTableRowOrigins.Regular,
          new Dictionary<string, MicrofilmCellValue> { ["sharedField"] = MicrofilmCellValue.FromText("preserved") }), actor));
      await Append(new RollMicrofilmRowCellChanged(rollId, Id.Unassigned, "row-1", MicrofilmTableRowOrigins.Regular,
        "sharedField", MicrofilmCellValue.FromText("durable"), actor));

      var row = (await GetQuery(RollMicrofilmRowQuery.CreateId(rollId, "row-1"))).Row;
      Assert.Equal("durable", row.Cells["sharedField"].Text);
      Assert.Equal("AJOHNSTON", row.CellAudits["sharedField"].LastChangedBy.ProcessUserId);
    }
  }
}
