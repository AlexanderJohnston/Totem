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
  public class MicrofilmTableTopicTests : TopicTests<MicrofilmTableTopic>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");
    static readonly Id ServerId = Id.From("00000000-0000-0000-0000-000000000201");

    [Fact]
    public async Task Seed_ForKnownClient_AppliesColumnsAndRegularRows()
    {
      await Append(ClientCreated());
      await Append(new SeedMicrofilmTable(ClientId, "demo", StarterColumns(), StarterRows()));

      var seeded = await Expect<MicrofilmTableSeeded>();

      Assert.Equal(ClientId, seeded.ClientId);
      Assert.Equal("demo", seeded.SeedId);
      Assert.Equal(new[] { "boxName", "rollName", "status", "reviewed" }, seeded.Columns.Select(column => column.Id));
      var row = Assert.Single(seeded.RegularRows);
      Assert.Equal("row-1", row.Id);
      Assert.Equal(MicrofilmTableRowOrigins.Regular, row.Origin);
      Assert.Equal("Box 01", row.Cells["boxName"].Text);
      Assert.False(row.Cells["reviewed"].Checkbox);
    }

    [Fact]
    public async Task ReplaceColumns_RejectsDuplicateColumnIds()
    {
      await Append(ClientCreated());
      await Append(new ReplaceMicrofilmTableColumns(ClientId, new List<MicrofilmTableColumn>
      {
        new("boxName", "Box", MicrofilmTableColumnTypes.Text, 160),
        new("boxName", "Duplicate", MicrofilmTableColumnTypes.Text, 160)
      }));

      var rejected = await Expect<MicrofilmTableColumnSchemaRejected>();

      Assert.Equal("DUPLICATE_COLUMN_ID", rejected.Code);
      Assert.Equal("boxName", rejected.ColumnId);
    }

    [Fact]
    public async Task UpdateRegularRowCell_ReturnsFullUpdatedRow()
    {
      await Append(ClientCreated());
      await Append(new SeedMicrofilmTable(ClientId, "demo", StarterColumns(), StarterRows()));
      await Expect<MicrofilmTableSeeded>();

      await Append(new UpdateMicrofilmRegularRowCell(ClientId, "row-1", "status", MicrofilmCellValue.FromText("Done")));

      var updated = await Expect<MicrofilmRegularRowCellUpdated>();

      Assert.Equal("row-1", updated.Row.Id);
      Assert.Equal(MicrofilmTableRowOrigins.Regular, updated.Row.Origin);
      Assert.Equal("Done", updated.Row.Cells["status"].Text);
      Assert.Equal("Box 01", updated.Row.Cells["boxName"].Text);
    }

    [Fact]
    public async Task UpdateRegularRowCell_RejectsInvalidDropdownValue()
    {
      await Append(ClientCreated());
      await Append(new SeedMicrofilmTable(ClientId, "demo", StarterColumns(), StarterRows()));
      await Expect<MicrofilmTableSeeded>();

      await Append(new UpdateMicrofilmRegularRowCell(ClientId, "row-1", "status", MicrofilmCellValue.FromText("Archived")));

      var rejected = await Expect<MicrofilmTableCellValueRejected>();

      Assert.Equal("row-1", rejected.RowId);
      Assert.Equal("status", rejected.ColumnId);
    }

    [Fact]
    public async Task CreateCustomRow_InitializesMissingCellsFromCurrentColumns()
    {
      await Append(ClientCreated());
      await Append(new SeedMicrofilmTable(ClientId, "demo", StarterColumns(), StarterRows()));
      await Expect<MicrofilmTableSeeded>();

      await Append(new CreateMicrofilmCustomRow(ClientId, new Dictionary<string, MicrofilmCellValue>
      {
        ["boxName"] = MicrofilmCellValue.FromText("Manual row")
      }));

      var created = await Expect<MicrofilmCustomRowCreated>();

      Assert.True(!string.IsNullOrWhiteSpace(created.Row.Id));
      Assert.Equal(MicrofilmTableRowOrigins.Custom, created.Row.Origin);
      Assert.Equal("Manual row", created.Row.Cells["boxName"].Text);
      Assert.Equal(MicrofilmCellValueKind.Null, created.Row.Cells["rollName"].Kind);
      Assert.False(created.Row.Cells["reviewed"].Checkbox);
    }

    [Fact]
    public async Task Write_ForUnknownClient_IsRejected()
    {
      await Append(new UpdateMicrofilmCustomRowCell(ClientId, "row-404", "boxName", MicrofilmCellValue.FromText("Ignored")));

      var rejected = await Expect<MicrofilmTableClientNotRecognized>();

      Assert.Equal(ClientId, rejected.ClientId);
    }

    static ClientCreated ClientCreated() =>
      new(new KnownClient("Job", "JOB-001", ClientId, ServerId));

    static List<MicrofilmTableColumn> StarterColumns() =>
      new()
      {
        new("boxName", "Box", MicrofilmTableColumnTypes.Text, 160),
        new("rollName", "Roll", MicrofilmTableColumnTypes.Text, 160),
        new("status", "Status", MicrofilmTableColumnTypes.Dropdown, 160, new[] { "New", "In Progress", "Done" }),
        new("reviewed", "Reviewed", MicrofilmTableColumnTypes.Checkbox, 120)
      };

    static List<MicrofilmTableRow> StarterRows() =>
      new()
      {
        new("row-1", MicrofilmTableRowOrigins.Regular, new Dictionary<string, MicrofilmCellValue>
        {
          ["boxName"] = MicrofilmCellValue.FromText("Box 01"),
          ["rollName"] = MicrofilmCellValue.FromText("Roll A"),
          ["status"] = MicrofilmCellValue.FromText("New"),
          ["reviewed"] = MicrofilmCellValue.FromCheckbox(false)
        })
      };
  }

  public class MicrofilmTableColumnsQueryTests : QueryTests<MicrofilmTableColumnsQuery>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");

    [Fact]
    public async Task KnownClientWithNoColumns_ReturnsEmptyColumns()
    {
      await Append(new ClientCreated(new KnownClient("Job", "JOB-001", ClientId, Id.Unassigned)));

      var query = await GetQuery(ClientId);

      Assert.Empty(query.Columns);
    }

    [Fact]
    public async Task Seed_ProjectsColumnsInOrder()
    {
      await Append(new ClientCreated(new KnownClient("Job", "JOB-001", ClientId, Id.Unassigned)));
      await Append(new MicrofilmTableSeeded(ClientId, "demo", MicrofilmTableTopicTestsStarter.Columns(), new List<MicrofilmTableRow>()));

      var query = await GetQuery(ClientId);

      Assert.Equal(new[] { "boxName", "rollName", "status", "reviewed" }, query.Columns.Select(column => column.Id));
    }
  }

  public class MicrofilmRegularRowsQueryTests : QueryTests<MicrofilmRegularRowsQuery>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");

    [Fact]
    public async Task Seed_ProjectsRegularRows()
    {
      await Append(new ClientCreated(new KnownClient("Job", "JOB-001", ClientId, Id.Unassigned)));
      await Append(new MicrofilmTableSeeded(ClientId, "demo", MicrofilmTableTopicTestsStarter.Columns(), MicrofilmTableTopicTestsStarter.Rows()));

      var query = await GetQuery(ClientId);
      var row = Assert.Single(query.Rows);

      Assert.Equal(MicrofilmTableRowOrigins.Regular, row.Origin);
      Assert.Equal("Box 01", row.Cells["boxName"].Text);
    }

    [Fact]
    public async Task CellUpdate_ReplacesOneRegularRow()
    {
      await Append(new ClientCreated(new KnownClient("Job", "JOB-001", ClientId, Id.Unassigned)));
      await Append(new MicrofilmTableSeeded(ClientId, "demo", MicrofilmTableTopicTestsStarter.Columns(), MicrofilmTableTopicTestsStarter.Rows()));
      await Append(new MicrofilmRegularRowCellUpdated(ClientId, new MicrofilmTableRow(
        "row-1",
        MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue>
        {
          ["boxName"] = MicrofilmCellValue.FromText("Box 01"),
          ["rollName"] = MicrofilmCellValue.FromText("Roll A"),
          ["status"] = MicrofilmCellValue.FromText("Done"),
          ["reviewed"] = MicrofilmCellValue.FromCheckbox(false)
        })));

      var query = await GetQuery(ClientId);
      var row = Assert.Single(query.Rows);

      Assert.Equal("Done", row.Cells["status"].Text);
    }
  }

  public class MicrofilmCustomRowsQueryTests : QueryTests<MicrofilmCustomRowsQuery>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");

    [Fact]
    public async Task CreatedAndUpdatedCustomRows_ProjectInEnvelope()
    {
      await Append(new ClientCreated(new KnownClient("Job", "JOB-001", ClientId, Id.Unassigned)));
      await Append(new MicrofilmCustomRowCreated(ClientId, new MicrofilmTableRow(
        "custom-1",
        MicrofilmTableRowOrigins.Custom,
        new Dictionary<string, MicrofilmCellValue> { ["boxName"] = MicrofilmCellValue.FromText("Manual") })));
      await Append(new MicrofilmCustomRowCellUpdated(ClientId, new MicrofilmTableRow(
        "custom-1",
        MicrofilmTableRowOrigins.Custom,
        new Dictionary<string, MicrofilmCellValue> { ["boxName"] = MicrofilmCellValue.FromText("Manual Updated") })));

      var query = await GetQuery(ClientId);
      var row = Assert.Single(query.CustomRows);

      Assert.Equal(MicrofilmTableRowOrigins.Custom, row.Origin);
      Assert.Equal("Manual Updated", row.Cells["boxName"].Text);
    }
  }

  public class MicrofilmClientLookupQueryTests : QueryTests<MicrofilmClientLookupQuery>
  {
    [Fact]
    public async Task TracksKnownServersAndClients()
    {
      var serverId = Id.From("00000000-0000-0000-0000-000000000201");
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ServerCreated(new KnownServer(@"\\server\film\", serverId)));
      await Append(new ClientCreated(new KnownClient("Job", "JOB-001", clientId, serverId)));

      var query = await GetQuery();

      Assert.True(query.HasServer(serverId));
      Assert.True(query.HasClient(clientId));
    }
  }

  static class MicrofilmTableTopicTestsStarter
  {
    public static List<MicrofilmTableColumn> Columns() =>
      new()
      {
        new("boxName", "Box", MicrofilmTableColumnTypes.Text, 160),
        new("rollName", "Roll", MicrofilmTableColumnTypes.Text, 160),
        new("status", "Status", MicrofilmTableColumnTypes.Dropdown, 160, new[] { "New", "In Progress", "Done" }),
        new("reviewed", "Reviewed", MicrofilmTableColumnTypes.Checkbox, 120)
      };

    public static List<MicrofilmTableRow> Rows() =>
      new()
      {
        new("row-1", MicrofilmTableRowOrigins.Regular, new Dictionary<string, MicrofilmCellValue>
        {
          ["boxName"] = MicrofilmCellValue.FromText("Box 01"),
          ["rollName"] = MicrofilmCellValue.FromText("Roll A"),
          ["status"] = MicrofilmCellValue.FromText("New"),
          ["reviewed"] = MicrofilmCellValue.FromCheckbox(false)
        })
      };
  }
}
