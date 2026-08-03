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
      MicrofilmProfileTestColumns.Create();
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
        MicrofilmProfileTestColumns.Create(),
        new System.DateTimeOffset(2026, 6, 10, 0, 0, 0, System.TimeSpan.FromHours(-4)),
        new System.DateTimeOffset(2026, 6, 10, 0, 0, 0, System.TimeSpan.FromHours(-4)));
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
      Assert.True(query.TryGetClient(clientId, out var client));
      Assert.Equal("JOB-001", client.JobNumber);
      Assert.Equal(serverId, client.ServerId);
    }
  }

  static class MicrofilmProfileTestColumns
  {
    public static List<MicrofilmTableColumn> Create() =>
      new()
      {
        new("boxName", "Box", MicrofilmTableColumnTypes.Text, 160),
        new("rollName", "Roll", MicrofilmTableColumnTypes.Text, 160),
        new("status", "Status", MicrofilmTableColumnTypes.Dropdown, 160, new[] { "New", "In Progress", "Done" }),
        new("reviewed", "Reviewed", MicrofilmTableColumnTypes.Checkbox, 120)
      };
  }
}
