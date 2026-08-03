using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Outermind.Microfilm.Topics;
using Totem;
using Totem.App.Tests;
using Xunit;

namespace Quantum.Tests
{
  public class RollMicrofilmTableTopicTests : TopicTests<RollMicrofilmTableTopic>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");
    static readonly Id BoxId = Id.From("00000000-0000-0000-0000-000000000301");
    static readonly Id RollId = Id.From("00000000-0000-0000-0000-000000000401");

    [Fact]
    public async Task CreateRollRow_UsesRollScopedAddressAndInitializesAuditState()
    {
      await Append(RollCreated());

      await Append(new CreateRollMicrofilmRow(
        RollId,
        ClientId,
        "row-1",
        MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue>
        {
          ["boxName"] = MicrofilmCellValue.FromText("Box 01"),
          ["rollName"] = MicrofilmCellValue.FromText("Roll A")
        },
        Actor()));

      var created = await Expect<RollMicrofilmRowCreated>();

      Assert.Equal(RollId, created.RollId);
      Assert.Equal(ClientId, created.ClientId);
      Assert.Equal("row-1", created.Row.Id);
      Assert.Equal(RollId.ToString(), created.Row.RollId);
      Assert.Equal(MicrofilmTableRowOrigins.Regular, created.Row.Origin);
      Assert.Equal("Box 01", created.Row.Cells["boxName"].Text);
      Assert.Equal("Roll A", created.Row.Cells["rollName"].Text);
      Assert.Equal(MicrofilmCellAuditStates.NotTrackedYet, created.Row.CellAudits["boxName"].State);
      Assert.Equal(MicrofilmCellAuditStates.NotTrackedYet, created.Row.CellAudits["rollName"].State);
    }

    [Fact]
    public async Task CreateRollRow_AcceptsSparseArbitraryScalarCellsAndAddsIdentityBaselines()
    {
      await Append(RollCreated());

      await Append(new CreateRollMicrofilmRow(
        RollId,
        ClientId,
        "optimistic-row",
        MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue> { [" arbitraryColumn "] = MicrofilmCellValue.FromCheckbox(true) },
        Actor()));

      var created = await Expect<RollMicrofilmRowCreated>();
      Assert.True(created.Row.Cells["arbitraryColumn"].Checkbox);
      Assert.Equal(MicrofilmCellValueKind.Null, created.Row.Cells["boxName"].Kind);
      Assert.Equal(MicrofilmCellValueKind.Null, created.Row.Cells["rollName"].Kind);
      Assert.Equal(MicrofilmCellAuditStates.NotTrackedYet, created.Row.CellAudits["arbitraryColumn"].State);
    }

    [Theory]
    [InlineData(MicrofilmTableRowOrigins.Regular)]
    [InlineData(MicrofilmTableRowOrigins.Custom)]
    public async Task CreateAndUpdate_UseTheSameRulesForEverySupportedOrigin(string origin)
    {
      await Append(RollCreated());
      await Append(new CreateRollMicrofilmRow(
        RollId,
        ClientId,
        "row-1",
        origin,
        new Dictionary<string, MicrofilmCellValue> { ["status"] = MicrofilmCellValue.FromText("New") },
        Actor()));

      var created = await Expect<RollMicrofilmRowCreated>();
      Assert.Equal(origin, created.Row.Origin);

      await Append(new UpdateRollMicrofilmRowCell(
        RollId,
        ClientId,
        "row-1",
        origin,
        "status",
        MicrofilmCellValue.FromText("Done"),
        Actor()));

      var changed = await Expect<RollMicrofilmRowCellChanged>();
      Assert.Equal(origin, changed.RowKind);
      Assert.Equal("Done", changed.Value.Text);
    }

    [Fact]
    public async Task UpdateRollRowCell_EmitsTargetedCellFactWithActor()
    {
      await Append(RollCreated());

      await Append(new CreateRollMicrofilmRow(
        RollId,
        ClientId,
        "row-1",
        MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue> { ["status"] = MicrofilmCellValue.FromText("New") },
        Actor()));
      await Expect<RollMicrofilmRowCreated>();

      await Append(new UpdateRollMicrofilmRowCell(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular, "status", MicrofilmCellValue.FromText("Done"), Actor()));

      var changed = await Expect<RollMicrofilmRowCellChanged>();

      Assert.Equal(RollId, changed.RollId);
      Assert.Equal("row-1", changed.RowId);
      Assert.Equal("status", changed.ColumnId);
      Assert.Equal("Done", changed.Value.Text);
      Assert.Equal("AJOHNSTON", changed.Actor.ProcessUserId);
    }

    [Fact]
    public async Task UpdateRollRowCell_AcceptsArbitraryTrimmedColumnId()
    {
      await Append(RollCreated());
      await Append(new CreateRollMicrofilmRow(
        RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue>(), Actor()));
      await Expect<RollMicrofilmRowCreated>();

      await Append(new UpdateRollMicrofilmRowCell(
        RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        " dynamicField ", MicrofilmCellValue.FromNumber(12), Actor()));

      var changed = await Expect<RollMicrofilmRowCellChanged>();
      Assert.Equal("dynamicField", changed.ColumnId);
      Assert.Equal(12, changed.Value.Number);
    }

    [Fact]
    public async Task UpdateRollRowCell_RejectsMismatchedRouteRowKind()
    {
      await Append(RollCreated());

      await Append(new CreateRollMicrofilmRow(
        RollId,
        ClientId,
        "row-1",
        MicrofilmTableRowOrigins.Custom,
        new Dictionary<string, MicrofilmCellValue> { ["status"] = MicrofilmCellValue.FromText("New") },
        Actor()));
      await Expect<RollMicrofilmRowCreated>();

      await Append(new UpdateRollMicrofilmRowCell(
        RollId,
        ClientId,
        "row-1",
        MicrofilmTableRowOrigins.Regular,
        "status",
        MicrofilmCellValue.FromText("Done"),
        Actor()));

      var rejected = await Expect<RollMicrofilmTableRowKindMismatch>();

      Assert.Equal("row-1", rejected.RowId);
      Assert.Equal(MicrofilmTableRowOrigins.Regular, rejected.ExpectedRowKind);
      Assert.Equal(MicrofilmTableRowOrigins.Custom, rejected.ActualRowKind);
    }

    [Fact]
    public async Task Write_ForUnknownRoll_IsRejected()
    {
      await Append(new UpdateRollMicrofilmRowCell(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular, "status", MicrofilmCellValue.FromText("Done"), Actor()));

      var rejected = await Expect<RollMicrofilmTableRollNotRecognized>();

      Assert.Equal(RollId, rejected.RollId);
    }

    static RollCreated RollCreated() =>
      new(new KnownRoll("Roll A", RollId, BoxId));

    static MicrofilmAuditActorStamp Actor() =>
      new("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie");
  }

  public class RollMicrofilmRowsQueryTests : QueryTests<RollMicrofilmRowsQuery>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");
    static readonly Id RollId = Id.From("00000000-0000-0000-0000-000000000401");

    [Fact]
    public async Task RollRowCellChange_ProjectsAuditMetadata()
    {
      await Append(new RollCreated(new KnownRoll("Roll A", RollId, Id.Unassigned)));
      await Append(new RollMicrofilmRowCreated(
        RollId,
        ClientId,
        new MicrofilmTableRow("row-1", RollId.ToString(), MicrofilmTableRowOrigins.Regular, new Dictionary<string, MicrofilmCellValue>
        {
          ["status"] = MicrofilmCellValue.FromText("New")
        }, new Dictionary<string, MicrofilmCellAudit>
        {
          ["status"] = MicrofilmCellAudit.NotTrackedYet()
        }),
        Actor()));
      await Append(new RollMicrofilmRowCellChanged(
        RollId,
        ClientId,
        "row-1",
        MicrofilmTableRowOrigins.Regular,
        "status",
        MicrofilmCellValue.FromText("Done"),
        Actor()));

      var query = await GetQuery(RollId);
      var row = Assert.Single(query.Rows);

      Assert.Equal("Done", row.Cells["status"].Text);
      Assert.Equal(MicrofilmCellAuditStates.Tracked, row.CellAudits["status"].State);
      Assert.Equal("AJOHNSTON", row.CellAudits["status"].LastChangedBy.ProcessUserId);
      Assert.True(row.CellAudits["status"].LastChangedAt.HasValue);
    }

    static MicrofilmAuditActorStamp Actor() =>
      new("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie");
  }

  public class RollMicrofilmRowQueryTests : QueryTests<RollMicrofilmRowQuery>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");
    static readonly Id RollId = Id.From("00000000-0000-0000-0000-000000000401");

    [Fact]
    public async Task RowQuery_IsAddressedByRollIdAndRowId()
    {
      await Append(new RollMicrofilmRowCreated(
        RollId,
        ClientId,
        new MicrofilmTableRow("row-1", RollId.ToString(), MicrofilmTableRowOrigins.Regular, new Dictionary<string, MicrofilmCellValue>
        {
          ["status"] = MicrofilmCellValue.FromText("New")
        }),
        Actor()));

      var query = await GetQuery(RollMicrofilmRowQuery.CreateId(RollId, "row-1"));

      Assert.Equal("row-1", query.Row.Id);
      Assert.Equal(RollId.ToString(), query.Row.RollId);
    }

    [Fact]
    public async Task RowQuery_DoesNotFallbackAcrossRollsOrMatchRollNameCells()
    {
      var otherRollId = Id.From("00000000-0000-0000-0000-000000000402");

      await Append(new RollMicrofilmRowCreated(
        RollId,
        ClientId,
        new MicrofilmTableRow("row-1", RollId.ToString(), MicrofilmTableRowOrigins.Custom, new Dictionary<string, MicrofilmCellValue>
        {
          ["rollName"] = MicrofilmCellValue.FromText(otherRollId.ToString())
        }),
        Actor()));

      var correct = await GetQuery(RollMicrofilmRowQuery.CreateId(RollId, "row-1"));
      var wrong = await GetQuery(RollMicrofilmRowQuery.CreateId(otherRollId, "row-1"));

      Assert.Equal(MicrofilmTableRowOrigins.Custom, correct.Row.Origin);
      Assert.Null(wrong.Row);
    }

    static MicrofilmAuditActorStamp Actor() =>
      new("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie");
  }

  public class RollMicrofilmLookupQueryTests : QueryTests<RollMicrofilmLookupQuery>
  {
    [Fact]
    public async Task TracksRollClientOwnership()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");
      var boxId = Id.From("00000000-0000-0000-0000-000000000301");
      var rollId = Id.From("00000000-0000-0000-0000-000000000401");

      await Append(new BoxCreated(new KnownBox("Box 01", boxId, clientId)));
      await Append(new RollCreated(new KnownRoll("Roll A", rollId, boxId)));

      var query = await GetQuery();

      Assert.True(query.TryGetRoll(rollId, out var roll));
      Assert.Equal(clientId, roll.ClientId);
      Assert.Equal(boxId, roll.BoxId);
    }

  }

}
