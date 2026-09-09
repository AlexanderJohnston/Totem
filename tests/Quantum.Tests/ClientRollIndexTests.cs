using System.Collections.Generic;
using System.Threading.Tasks;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Totem;
using Totem.App.Tests;
using Xunit;

namespace Quantum.Tests
{
  public class ClientRollIndexQueryTests : QueryTests<ClientRollIndexQuery>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");
    static readonly Id BoxId = Id.From("00000000-0000-0000-0000-000000000201");
    static readonly Id RollId = RollIds.From(ClientId, BoxId, "Roll A");

    [Fact]
    public async Task ClientHierarchyAndRowChanges_UpdateRollIndex()
    {
      await Append(new ClientCreated(new KnownClient("Client", "JOB-001", ClientId, Id.Unassigned)));
      await Append(new BoxCreated(new KnownBox("Box 1", BoxId, ClientId)));
      await Append(new RollCreated(new KnownRoll("Roll A", RollId, BoxId), ClientId));
      await Append(new RollMicrofilmRowCreated(
        RollId,
        ClientId,
        new MicrofilmTableRow("row-1", RollId.ToString(), MicrofilmTableRowOrigins.Regular, new Dictionary<string, MicrofilmCellValue>()),
        Actor()));
      await Append(new RollMicrofilmRowCellChanged(
        RollId,
        ClientId,
        "row-1",
        MicrofilmTableRowOrigins.Regular,
        "status",
        MicrofilmCellValue.FromText("Done"),
        Actor()));

      var query = await GetQuery(ClientId);
      var roll = Assert.Single(query.RollsById).Value;

      Assert.Equal(ClientId, query.ClientId);
      Assert.Contains(BoxId, query.BoxIds);
      Assert.Equal(RollId, roll.RollId);
      Assert.Equal(BoxId, roll.BoxId);
      Assert.Equal(2, roll.TableVersion);
    }

    [Fact]
    public async Task WaspRollBatch_SeedsImportedRollIds()
    {
      await Append(new WaspBoxRollsIdentified(
        "JOB-001",
        ClientId,
        BoxId,
        new List<WaspAcceptedRollAsset>
        {
          new("JOB-001-Box 1-Roll A", "1", "Roll A"),
          new("JOB-001-Box 1-Roll B", "1", "Roll B")
        }));

      var query = await GetQuery(ClientId);

      Assert.Contains(BoxId, query.BoxIds);
      Assert.Collection(
        query.RollsById,
        roll => Assert.Equal(0, roll.Value.TableVersion),
        roll => Assert.Equal(0, roll.Value.TableVersion));
      Assert.Contains(RollIds.From(ClientId, BoxId, "Roll A").ToString(), query.RollsById.Keys);
      Assert.Contains(RollIds.From(ClientId, BoxId, "Roll B").ToString(), query.RollsById.Keys);
    }

    [Fact]
    public async Task RowChange_CanStartIndexForExistingRoll()
    {
      await Append(new RollMicrofilmRowCellChanged(
        RollId,
        ClientId,
        "row-1",
        MicrofilmTableRowOrigins.Regular,
        "status",
        MicrofilmCellValue.FromText("Done"),
        Actor()));

      var query = await GetQuery(ClientId);
      var roll = Assert.Single(query.RollsById).Value;

      Assert.Equal(RollId, roll.RollId);
      Assert.Equal(Id.Unassigned, roll.BoxId);
      Assert.Equal(1, roll.TableVersion);
    }

    static MicrofilmAuditActorStamp Actor() =>
      new("identified", "Test User", "TEST", "test");
  }
}
