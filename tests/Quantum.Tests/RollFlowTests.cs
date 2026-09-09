using System;
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
  public class RollManagerTopicTests : TopicTests<RollManagerTopic>
  {
    [Fact]
    public async Task BoxRollBatch_CreatesAllMissingRollsWithCompositeIds()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");
      var box = new KnownBox("1", Id.From("00000000-0000-0000-0000-000000000201"), clientId);

      await Append(new BoxCreated(box));
      await Append(new WaspBoxRollsIdentified(
        "JOB-001",
        clientId,
        box.BoxId,
        new List<WaspAcceptedRollAsset>
        {
          new("JOB-001-Box 1-APP-41", "1", "APP-41"),
          new("JOB-001-Box 1-APP-42", "1", "APP-42")
        }));

      var created = new[]
      {
        await Expect<RollCreated>(),
        await Expect<RollCreated>()
      }.OrderBy(e => e.Roll.RollName).ToList();

      Assert.Collection(created,
        e =>
        {
          Assert.Equal("APP-41", e.Roll.RollName);
          Assert.Equal(RollIds.From(clientId, box.BoxId, "APP-41"), e.Roll.RollId);
          Assert.Equal(box.BoxId, e.Roll.BoxId);
          Assert.Equal(clientId, e.ClientId);
        },
        e =>
        {
          Assert.Equal("APP-42", e.Roll.RollName);
          Assert.Equal(RollIds.From(clientId, box.BoxId, "APP-42"), e.Roll.RollId);
          Assert.Equal(box.BoxId, e.Roll.BoxId);
          Assert.Equal(clientId, e.ClientId);
        });
    }

    [Fact]
    public async Task BoxRollBatch_SkipsDuplicateNamesAlreadyKnownOrRepeatedInBatch()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");
      var box = new KnownBox("1", Id.From("00000000-0000-0000-0000-000000000201"), clientId);

      await Append(new BoxCreated(box));
      await Append(new RollCreated(new KnownRoll("APP-41", RollIds.From(clientId, box.BoxId, "APP-41"), box.BoxId)));
      await Append(new WaspBoxRollsIdentified(
        "JOB-001",
        clientId,
        box.BoxId,
        new List<WaspAcceptedRollAsset>
        {
          new("JOB-001-Box 1-APP-41", "1", "APP-41"),
          new("JOB-001-Box 1-APP-41-Duplicate", "1", "APP-41"),
          new("JOB-001-Box 1-APP-42", "1", "APP-42")
        }));

      var created = await Expect<RollCreated>();

      Assert.Equal("APP-42", created.Roll.RollName);
      Assert.Equal(RollIds.From(clientId, box.BoxId, "APP-42"), created.Roll.RollId);

      var ex = await Assert.ThrowsAsync<ExpectException>(async () => await Expect<RollCreated>(200));
      Assert.IsType<TimeoutException>(ex.InnerException);
    }
  }

  public class BoxStatusQueryTests : QueryTests<BoxStatusQuery>
  {
    [Fact]
    public async Task RollCreated_AddsAllRollsForTheBox()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");
      var box = new KnownBox("1", Id.From("00000000-0000-0000-0000-000000000201"), clientId);

      await Append(new BoxCreated(box));
      await Append(new RollCreated(new KnownRoll("APP-41", RollIds.From(clientId, box.BoxId, "APP-41"), box.BoxId)));
      await Append(new RollCreated(new KnownRoll("APP-42", RollIds.From(clientId, box.BoxId, "APP-42"), box.BoxId)));

      var query = await GetQuery(box.BoxId);
      var rolls = query.Rolls.OrderBy(r => r.RollName).ToList();

      Assert.Equal(box.BoxId, query.Box.BoxId);
      Assert.Collection(rolls,
        roll =>
        {
          Assert.Equal("APP-41", roll.RollName);
          Assert.Equal(RollIds.From(clientId, box.BoxId, "APP-41"), roll.RollId);
        },
        roll =>
        {
          Assert.Equal("APP-42", roll.RollName);
          Assert.Equal(RollIds.From(clientId, box.BoxId, "APP-42"), roll.RollId);
        });
    }
  }

  public class RollStatusQueryTests : QueryTests<RollStatusQuery>
  {
    [Fact]
    public async Task SameRollNameInDifferentBoxes_UsesDistinctQueryInstances()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");
      var rollName = "Cartridge 6";
      var firstBoxId = Id.From("00000000-0000-0000-0000-000000000201");
      var secondBoxId = Id.From("00000000-0000-0000-0000-000000000202");
      var firstRoll = new KnownRoll(rollName, RollIds.From(clientId, firstBoxId, rollName), firstBoxId);
      var secondRoll = new KnownRoll(rollName, RollIds.From(clientId, secondBoxId, rollName), secondBoxId);

      Assert.NotEqual(firstRoll.RollId, secondRoll.RollId);

      await Append(new RollCreated(firstRoll));
      await Append(new RollCreated(secondRoll));

      var firstQuery = await GetQuery(firstRoll.RollId);
      var secondQuery = await GetQuery(secondRoll.RollId);

      Assert.Equal(firstBoxId, firstQuery.Roll.BoxId);
      Assert.Equal(firstRoll.RollId, firstQuery.Roll.RollId);
      Assert.Equal(secondBoxId, secondQuery.Roll.BoxId);
      Assert.Equal(secondRoll.RollId, secondQuery.Roll.RollId);
    }
  }
}
