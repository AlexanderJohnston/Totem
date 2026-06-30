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
  public class MicrofilmClientProfileTopicTests : TopicTests<MicrofilmClientProfileTopic>
  {
    [Fact]
    public async Task CreateProfile_AcceptsNormalizedNameDescriptionAndColumns()
    {
      await Append(new CreateMicrofilmClientProfile("  WASP Import  ", "  Imported rows  ", StarterColumns()));

      var created = await Expect<MicrofilmClientProfileCreated>();

      Assert.False(string.IsNullOrWhiteSpace(created.Profile.Id));
      Assert.Equal("WASP Import", created.Profile.Name);
      Assert.Equal("Imported rows", created.Profile.Description);
      Assert.Equal(new[] { "boxName", "rollName", "status", "reviewed" }, created.Profile.Columns.Select(column => column.Id));
      Assert.Equal(created.Profile.CreatedAt, created.Profile.UpdatedAt);
    }

    [Fact]
    public async Task CreateProfile_RejectsEmptyName()
    {
      await Append(new CreateMicrofilmClientProfile("  ", null, StarterColumns()));

      var rejected = await Expect<MicrofilmClientProfileNameRejected>();

      Assert.Equal("INVALID_PROFILE_NAME", rejected.Code);
    }

    [Fact]
    public async Task CreateProfile_RejectsDuplicateNameCaseInsensitively()
    {
      await Append(new MicrofilmClientProfileCreated(Profile("profile-1", "WASP Import")));

      await Append(new CreateMicrofilmClientProfile("wasp import", null, StarterColumns()));

      var duplicated = await Expect<MicrofilmClientProfileNameDuplicated>();

      Assert.Equal("wasp import", duplicated.Name);
    }

    [Fact]
    public async Task CreateProfile_RejectsInvalidColumns()
    {
      await Append(new CreateMicrofilmClientProfile("Invalid", null, new List<MicrofilmTableColumn>
      {
        new("boxName", "Box", MicrofilmTableColumnTypes.Text, 160),
        new("boxName", "Duplicate", MicrofilmTableColumnTypes.Text, 160)
      }));

      var rejected = await Expect<MicrofilmClientProfileColumnsRejected>();

      Assert.Equal("DUPLICATE_COLUMN_ID", rejected.Code);
      Assert.Equal("boxName", rejected.ColumnId);
    }

    [Fact]
    public async Task ReplaceProfile_UpdatesSnapshotAndPreservesCreatedAt()
    {
      var profile = Profile("profile-1", "WASP Import");
      await Append(new MicrofilmClientProfileCreated(profile));

      await Append(new ReplaceMicrofilmClientProfile("profile-1", "  Miller Default  ", "  ", new List<MicrofilmTableColumn>
      {
        new("boxName", "Box", MicrofilmTableColumnTypes.Text, 160)
      }));

      var replaced = await Expect<MicrofilmClientProfileReplaced>();

      Assert.Equal("profile-1", replaced.Profile.Id);
      Assert.Equal("Miller Default", replaced.Profile.Name);
      Assert.Null(replaced.Profile.Description);
      Assert.Single(replaced.Profile.Columns);
      Assert.Equal(profile.CreatedAt, replaced.Profile.CreatedAt);
      Assert.True(replaced.Profile.UpdatedAt >= replaced.Profile.CreatedAt);
    }

    [Fact]
    public async Task ReplaceProfile_AllowsExistingProfileName()
    {
      await Append(new MicrofilmClientProfileCreated(Profile("profile-1", "WASP Import")));

      await Append(new ReplaceMicrofilmClientProfile("profile-1", "wasp import", null, StarterColumns()));

      var replaced = await Expect<MicrofilmClientProfileReplaced>();

      Assert.Equal("wasp import", replaced.Profile.Name);
    }

    [Fact]
    public async Task ReplaceProfile_RejectsUnknownProfile()
    {
      await Append(new ReplaceMicrofilmClientProfile("missing", "WASP Import", null, StarterColumns()));

      var rejected = await Expect<MicrofilmClientProfileNotRecognized>();

      Assert.Equal("missing", rejected.ProfileId);
    }

    [Fact]
    public async Task ReplaceProfile_RejectsDuplicateNameOwnedByAnotherProfile()
    {
      await Append(new MicrofilmClientProfileCreated(Profile("profile-1", "WASP Import")));
      await Append(new MicrofilmClientProfileCreated(Profile("profile-2", "Miller Default")));

      await Append(new ReplaceMicrofilmClientProfile("profile-2", "wasp import", null, StarterColumns()));

      var duplicated = await Expect<MicrofilmClientProfileNameDuplicated>();

      Assert.Equal("wasp import", duplicated.Name);
    }

    [Fact]
    public async Task DeleteProfile_RemovesNameFromCatalog()
    {
      await Append(new MicrofilmClientProfileCreated(Profile("profile-1", "WASP Import")));

      await Append(new DeleteMicrofilmClientProfile("profile-1"));
      await Expect<MicrofilmClientProfileDeleted>();

      await Append(new CreateMicrofilmClientProfile("WASP Import", null, StarterColumns()));
      var created = await Expect<MicrofilmClientProfileCreated>();

      Assert.Equal("WASP Import", created.Profile.Name);
    }

    [Fact]
    public async Task DeleteUnknownProfile_ReturnsUnknownProfile()
    {
      await Append(new DeleteMicrofilmClientProfile("missing"));

      var rejected = await Expect<MicrofilmClientProfileNotRecognized>();

      Assert.Equal("missing", rejected.ProfileId);
    }

    static MicrofilmClientProfile Profile(string id, string name) =>
      new(
        id,
        name,
        null,
        StarterColumns(),
        new System.DateTimeOffset(2026, 6, 10, 0, 0, 0, System.TimeSpan.FromHours(-4)),
        new System.DateTimeOffset(2026, 6, 10, 0, 0, 0, System.TimeSpan.FromHours(-4)));

    static List<MicrofilmTableColumn> StarterColumns() =>
      MicrofilmTableTopicTestsStarter.Columns();
  }

  public class MicrofilmClientProfilesQueryTests : QueryTests<MicrofilmClientProfilesQuery>
  {
    [Fact]
    public async Task CreatedProfiles_AppearOrderedByNameThenId()
    {
      await Append(new MicrofilmClientProfileCreated(Profile("profile-b", "Zulu")));
      await Append(new MicrofilmClientProfileCreated(Profile("profile-a", "Alpha")));

      var query = await GetQuery();

      Assert.Equal(new[] { "profile-a", "profile-b" }, query.Profiles.Select(profile => profile.Id));
    }

    [Fact]
    public async Task ReplacedProfiles_UpdateInPlaceAndResort()
    {
      await Append(new MicrofilmClientProfileCreated(Profile("profile-a", "Alpha")));
      await Append(new MicrofilmClientProfileCreated(Profile("profile-z", "Zulu")));
      await Append(new MicrofilmClientProfileReplaced(Profile("profile-z", "Aardvark")));

      var query = await GetQuery();

      Assert.Equal(new[] { "profile-z", "profile-a" }, query.Profiles.Select(profile => profile.Id));
      Assert.Equal("Aardvark", query.Profiles[0].Name);
    }

    [Fact]
    public async Task DeletedProfiles_Disappear()
    {
      await Append(new MicrofilmClientProfileCreated(Profile("profile-a", "Alpha")));
      await Append(new MicrofilmClientProfileDeleted("profile-a"));

      var query = await GetQuery();

      Assert.Empty(query.Profiles);
    }

    [Fact]
    public async Task OrderingTieBreaksById()
    {
      await Append(new MicrofilmClientProfileCreated(Profile("profile-b", "Same")));
      await Append(new MicrofilmClientProfileCreated(Profile("profile-a", "same")));

      var query = await GetQuery();

      Assert.Equal(new[] { "profile-a", "profile-b" }, query.Profiles.Select(profile => profile.Id));
    }

    static MicrofilmClientProfile Profile(string id, string name) =>
      new(
        id,
        name,
        null,
        MicrofilmTableTopicTestsStarter.Columns(),
        new System.DateTimeOffset(2026, 6, 10, 0, 0, 0, System.TimeSpan.FromHours(-4)),
        new System.DateTimeOffset(2026, 6, 10, 0, 0, 0, System.TimeSpan.FromHours(-4)));
  }

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
    public async Task CreateRegularRow_AcceptsFrontendStableRowId()
    {
      await Append(ClientCreated());
      await Append(new ReplaceMicrofilmTableColumns(ClientId, StarterColumns()));
      await Expect<MicrofilmTableColumnsChanged>();

      await Append(new CreateMicrofilmRegularRow(ClientId, "wasp-box-1", new Dictionary<string, MicrofilmCellValue>
      {
        ["boxName"] = MicrofilmCellValue.FromText("Box 01"),
        ["status"] = MicrofilmCellValue.FromText("New")
      }));

      var created = await Expect<MicrofilmRegularRowCreated>();

      Assert.Equal(ClientId, created.ClientId);
      Assert.Equal("wasp-box-1", created.Row.Id);
      Assert.Equal(MicrofilmTableRowOrigins.Regular, created.Row.Origin);
      Assert.Equal("Box 01", created.Row.Cells["boxName"].Text);
      Assert.Equal("New", created.Row.Cells["status"].Text);
      Assert.Equal(MicrofilmCellValueKind.Null, created.Row.Cells["rollName"].Kind);
      Assert.False(created.Row.Cells["reviewed"].Checkbox);
    }

    [Fact]
    public async Task CreateRegularRow_RejectsDuplicateRowId()
    {
      await Append(ClientCreated());
      await Append(new SeedMicrofilmTable(ClientId, "demo", StarterColumns(), StarterRows()));
      await Expect<MicrofilmTableSeeded>();

      await Append(new CreateMicrofilmRegularRow(ClientId, "row-1", new Dictionary<string, MicrofilmCellValue>
      {
        ["boxName"] = MicrofilmCellValue.FromText("Duplicate")
      }));

      var conflict = await Expect<MicrofilmTableRowConflict>();

      Assert.Equal(ClientId, conflict.ClientId);
      Assert.Equal("row-1", conflict.RowId);
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
    public async Task CreatedRegularRow_ProjectsInRows()
    {
      await Append(new ClientCreated(new KnownClient("Job", "JOB-001", ClientId, Id.Unassigned)));
      await Append(new MicrofilmRegularRowCreated(ClientId, new MicrofilmTableRow(
        "wasp-box-1",
        MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue>
        {
          ["boxName"] = MicrofilmCellValue.FromText("Box 01"),
          ["status"] = MicrofilmCellValue.FromText("New")
        })));

      var query = await GetQuery(ClientId);
      var row = Assert.Single(query.Rows);

      Assert.Equal("wasp-box-1", row.Id);
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
