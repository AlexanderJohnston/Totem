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
    public async Task UpdateRollRowCell_EmitsTargetedCellFactWithActor()
    {
      await Append(RollCreated());
      await Append(new ReplaceRollMicrofilmTableColumns(RollId, ClientId, Columns()));
      await Expect<RollMicrofilmTableColumnsChanged>();

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
    public async Task UpdateRollRowCell_RejectsMismatchedRouteRowKind()
    {
      await Append(RollCreated());
      await Append(new ReplaceRollMicrofilmTableColumns(RollId, ClientId, Columns()));
      await Expect<RollMicrofilmTableColumnsChanged>();

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

    static List<MicrofilmTableColumn> Columns() =>
      new()
      {
        new("boxName", "Box", MicrofilmTableColumnTypes.Text, 160),
        new("status", "Status", MicrofilmTableColumnTypes.Dropdown, 160, new[] { "New", "Done" })
      };

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

    public class RollMicrofilmColumnsQueryTests : QueryTests<RollMicrofilmColumnsQuery>
    {
      static readonly Id RollId = Id.From("00000000-0000-0000-0000-000000000401");

      [Fact]
      public async Task RollCreated_SeedsDefaultBoxAndRollColumns()
      {
        await Append(new RollCreated(new KnownRoll("Roll A", RollId, Id.Unassigned)));

        var query = await GetQuery(RollId);

        Assert.Collection(
          query.Columns,
          column =>
          {
            Assert.Equal("boxName", column.Id);
            Assert.Equal(MicrofilmTableColumnTypes.Text, column.Type);
          },
          column =>
          {
            Assert.Equal("rollName", column.Id);
            Assert.Equal(MicrofilmTableColumnTypes.Text, column.Type);
          });
      }
    }

    static List<MicrofilmTableColumn> Columns() =>
      new()
      {
        new("status", "Status", MicrofilmTableColumnTypes.Dropdown, 160, new[] { "New", "Done" })
      };

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

    public class LegacyRowRoutingIndexQueryTests : QueryTests<LegacyRowRoutingIndexQuery>
    {
      [Fact]
      public async Task CreatedRollRows_CanResolveLegacyClientRowRoute()
      {
        var clientId = Id.From("00000000-0000-0000-0000-000000000101");
        var rollId = Id.From("00000000-0000-0000-0000-000000000401");

        await Append(new RollMicrofilmRowCreated(
          rollId,
          clientId,
          new MicrofilmTableRow("row-1", rollId.ToString(), MicrofilmTableRowOrigins.Regular, new Dictionary<string, MicrofilmCellValue>()),
          new MicrofilmAuditActorStamp("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie")));

        var query = await GetQuery();

        Assert.True(query.TryGetRoute(clientId, "row-1", out var route));
        Assert.Equal(rollId, route.RollId);
        Assert.Equal(MicrofilmTableRowOrigins.Regular, route.RowKind);
      }
    }
  }

  public class RollMicrofilmLegacyProjectionTests : QueryTests<MicrofilmRegularRowsQuery>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");
    static readonly Id RollId = Id.From("00000000-0000-0000-0000-000000000401");

    [Fact]
    public async Task RollScopedRegularRows_ProjectToLegacyClientRowsDuringMigration()
    {
      await Append(new ClientCreated(new KnownClient("Job", "JOB-001", ClientId, Id.Unassigned)));
      await Append(new RollMicrofilmRowCreated(
        RollId,
        ClientId,
        new MicrofilmTableRow("row-1", RollId.ToString(), MicrofilmTableRowOrigins.Regular, new Dictionary<string, MicrofilmCellValue>
        {
          ["status"] = MicrofilmCellValue.FromText("New")
        }),
        new MicrofilmAuditActorStamp("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie")));

      var query = await GetQuery(ClientId);
      var row = Assert.Single(query.Rows);

      Assert.Equal("row-1", row.Id);
      Assert.Equal(RollId.ToString(), row.RollId);
      Assert.Equal("New", row.Cells["status"].Text);
    }
  }
}
