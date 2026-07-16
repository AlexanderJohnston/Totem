using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Outermind.Microfilm.Topics;
using Totem;
using Totem.Timeline.Area;
using Totem.App.Tests;
using Xunit;

namespace Quantum.Tests
{
  public class RollMicrofilmClientCatalogRegressionTests : TopicTests<RollMicrofilmTableTopic>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");
    static readonly Id RollId = Id.From("00000000-0000-0000-0000-000000000401");

    [Fact]
    public async Task UpdateRollRowCell_UsesClientCatalogSnapshotInsteadOfRollDefaults()
    {
      var catalog = new List<MicrofilmTableColumn>
      {
        new("status", "Status", MicrofilmTableColumnTypes.Dropdown, 160, new[] { "New", "Done" })
      };

      await Append(new RollCreated(new KnownRoll("Roll A", RollId, Id.Unassigned)));
      await Append(new CreateRollMicrofilmRow(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue> { ["status"] = MicrofilmCellValue.FromText("New") }, Actor(), catalog));
      await Expect<RollMicrofilmRowCreated>();

      await Append(new UpdateRollMicrofilmRowCell(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        "status", MicrofilmCellValue.FromText("Done"), Actor(), catalog));

      Assert.Equal("Done", (await Expect<RollMicrofilmRowCellChanged>()).Value.Text);
    }

    [Fact]
    public async Task UpdateRollRowCell_RejectsFieldOutsideClientCatalogSnapshot()
    {
      var catalog = new List<MicrofilmTableColumn> { new("status", "Status", MicrofilmTableColumnTypes.Text, 160) };
      await Append(new RollCreated(new KnownRoll("Roll A", RollId, Id.Unassigned)));
      await Append(new CreateRollMicrofilmRow(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue>(), Actor(), catalog));
      await Expect<RollMicrofilmRowCreated>();

      await Append(new UpdateRollMicrofilmRowCell(RollId, ClientId, "row-1", MicrofilmTableRowOrigins.Regular,
        "retiredField", MicrofilmCellValue.FromText("ignored"), Actor(), catalog));

      Assert.Equal("retiredField", (await Expect<MicrofilmTableColumnNotRecognized>()).ColumnId);
    }

    static MicrofilmAuditActorStamp Actor() =>
      new("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie");
  }

  public class MicrofilmCatalogRetentionTests
  {
    [Fact]
    public void ReconcileCellsAndAudits_PreserveRemovedFieldUntilItIsReadded()
    {
      var status = new MicrofilmTableColumn("status", "Status", MicrofilmTableColumnTypes.Text, 160);
      var cells = new Dictionary<string, MicrofilmCellValue> { ["status"] = MicrofilmCellValue.FromText("Completed") };
      var audits = new Dictionary<string, MicrofilmCellAudit>
      {
        ["status"] = MicrofilmCellAudit.Tracked(
          new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero),
          new MicrofilmAuditActorStamp("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie"))
      };

      var inactiveCells = MicrofilmTableRules.ReconcileCells(new List<MicrofilmTableColumn>(), cells);
      var inactiveAudits = MicrofilmTableRules.ReconcileCellAudits(new List<MicrofilmTableColumn>(), audits);
      var readdedCells = MicrofilmTableRules.ReconcileCells(new[] { status }, inactiveCells);
      var readdedAudits = MicrofilmTableRules.ReconcileCellAudits(new[] { status }, inactiveAudits);

      Assert.Equal("Completed", readdedCells["status"].Text);
      Assert.Equal(MicrofilmCellAuditStates.Tracked, readdedAudits["status"].State);
      Assert.Equal("AJOHNSTON", readdedAudits["status"].LastChangedBy.ProcessUserId);
    }
  }

  public class EffectiveRollCatalogTests
  {
    [Fact]
    public void MergeRollCatalog_PreservesBaselineOrderAndLetsClientOverrideBaselineDefinitions()
    {
      var clientBox = new MicrofilmTableColumn("boxName", "Container", MicrofilmTableColumnTypes.Text, 240);
      var catalog = new List<MicrofilmTableColumn>
      {
        clientBox,
        new("status", "Status", MicrofilmTableColumnTypes.Dropdown, 160, new[] { "New", "Done" })
      };

      var effective = MicrofilmDefaultColumns.MergeRollCatalog(catalog);
      clientBox.Name = "Mutated after merge";

      Assert.Collection(
        effective,
        column =>
        {
          Assert.Equal("boxName", column.Id);
          Assert.Equal("Container", column.Name);
          Assert.True(column.Width.HasValue);
          Assert.Equal(240d, column.Width.Value);
        },
        column =>
        {
          Assert.Equal("rollName", column.Id);
          Assert.Equal("Roll", column.Name);
        },
        column => Assert.Equal("status", column.Id));
    }
  }

  public class RollMicrofilmTableAggregateQueryTests : QueryTests<RollMicrofilmTableQuery>
  {
    [Fact]
    public async Task AggregateRetainsTheSameDurableValueAndAuditAsRowQueries()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");
      var rollId = Id.From("00000000-0000-0000-0000-000000000401");
      var actor = new MicrofilmAuditActorStamp("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie");
      await Append(new RollCreated(new KnownRoll("Roll A", rollId, Id.Unassigned)));
      await Append(new RollMicrofilmRowCreated(rollId, clientId,
        new MicrofilmTableRow("row-1", rollId.ToString(), MicrofilmTableRowOrigins.Regular,
          new Dictionary<string, MicrofilmCellValue> { ["status"] = MicrofilmCellValue.FromText("New") }), actor));
      await Append(new RollMicrofilmRowCellChanged(rollId, clientId, "row-1", MicrofilmTableRowOrigins.Regular,
        "status", MicrofilmCellValue.FromText("Done"), actor));

      var row = Assert.Single((await GetQuery(rollId)).Rows);
      Assert.Equal("Done", row.Cells["status"].Text);
      Assert.Equal(MicrofilmCellAuditStates.Tracked, row.CellAudits["status"].State);
      Assert.Equal("AJOHNSTON", row.CellAudits["status"].LastChangedBy.ProcessUserId);
    }
  }

  public class RollColumnCompatibilityObservationTests
  {
    [Fact]
    public void MicrofilmTableColumnsQuery_DoesNotObserveRollColumnChanges()
    {
      AssertDoesNotObserveRollColumnChanges<MicrofilmTableColumnsQuery>();
    }

    [Fact]
    public void MicrofilmRegularRowsQuery_DoesNotObserveRollColumnChanges()
    {
      AssertDoesNotObserveRollColumnChanges<MicrofilmRegularRowsQuery>();
    }

    [Fact]
    public void MicrofilmCustomRowsQuery_DoesNotObserveRollColumnChanges()
    {
      AssertDoesNotObserveRollColumnChanges<MicrofilmCustomRowsQuery>();
    }

    static void AssertDoesNotObserveRollColumnChanges<TQuery>()
    {
      var area = AreaMap.From(typeof(TQuery).Assembly.GetTypes());
      QueryType queryType = area.Queries.Get<TQuery>();

      Assert.False(queryType.Observations.Contains(typeof(RollMicrofilmTableColumnsChanged)));
    }
  }
}