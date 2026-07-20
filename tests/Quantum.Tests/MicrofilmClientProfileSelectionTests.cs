using System.Threading.Tasks;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Outermind.Microfilm.Topics;
using Totem;
using Totem.App.Tests;
using Xunit;

namespace Quantum.Tests
{
  public class MicrofilmClientProfileSelectionTopicTests : TopicTests<MicrofilmClientProfileSelectionTopic>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000301");

    [Fact]
    public async Task SetReplaceAndClear_ProfileSelection()
    {
      await Append(ClientCreated());

      await Append(new SetMicrofilmClientProfileSelection(ClientId, "  profile-a  "));
      Assert.Equal("profile-a", (await Expect<MicrofilmClientProfileSelectionChanged>()).ProfileId);

      await Append(new SetMicrofilmClientProfileSelection(ClientId, "profile-b"));
      Assert.Equal("profile-b", (await Expect<MicrofilmClientProfileSelectionChanged>()).ProfileId);

      await Append(new SetMicrofilmClientProfileSelection(ClientId, null));
      Assert.Null((await Expect<MicrofilmClientProfileSelectionChanged>()).ProfileId);
    }

    [Fact]
    public async Task Set_ForUnknownClient_IsRejected()
    {
      await Append(new SetMicrofilmClientProfileSelection(ClientId, "profile-a"));

      Assert.Equal(ClientId, (await Expect<MicrofilmTableClientNotRecognized>()).ClientId);
    }

    [Fact]
    public async Task WhitespaceProfileId_IsRejectedWithoutChangingSelection()
    {
      await Append(ClientCreated());
      await Append(new SetMicrofilmClientProfileSelection(ClientId, "profile-a"));
      await Expect<MicrofilmClientProfileSelectionChanged>();

      await Append(new SetMicrofilmClientProfileSelection(ClientId, "  "));
      await Append(new SetMicrofilmClientProfileSelection(ClientId, "profile-b"));

      Assert.Equal("profile-b", (await Expect<MicrofilmClientProfileSelectionChanged>()).ProfileId);
    }

    static ClientCreated ClientCreated() =>
      new(new KnownClient("Job", "JOB-001", ClientId, Id.Unassigned));
  }

  public class MicrofilmClientProfileSelectionQueryTests : QueryTests<MicrofilmClientProfileSelectionQuery>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000301");

    [Fact]
    public async Task ClientCreated_ProjectsInitialNullSelection()
    {
      await Append(ClientCreated());

      var query = await GetQuery(ClientId);

      Assert.Equal(ClientId, query.ClientId);
      Assert.Null(query.ProfileId);
    }

    [Fact]
    public async Task SelectionChanges_ProjectSetReplaceAndClear()
    {
      await Append(ClientCreated());
      await Append(new MicrofilmClientProfileSelectionChanged(ClientId, "profile-a"));
      Assert.Equal("profile-a", (await GetQuery(ClientId)).ProfileId);

      await Append(new MicrofilmClientProfileSelectionChanged(ClientId, "profile-b"));
      Assert.Equal("profile-b", (await GetQuery(ClientId)).ProfileId);

      await Append(new MicrofilmClientProfileSelectionChanged(ClientId, null));
      Assert.Null((await GetQuery(ClientId)).ProfileId);
    }

    static ClientCreated ClientCreated() =>
      new(new KnownClient("Job", "JOB-001", ClientId, Id.Unassigned));
  }
}
