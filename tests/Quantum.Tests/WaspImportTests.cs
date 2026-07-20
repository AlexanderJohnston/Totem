using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Outermind.Microfilm.Topics;
using Totem;
using Totem.App.Tests;
using Xunit;

namespace Quantum.Tests
{
  public class WaspImportTopicTests : TopicTests<WaspImportTopic>
  {
    readonly FakeWaspAssetService _waspService = new();

    public WaspImportTopicTests()
    {
      Services.AddSingleton<IWaspAssetService>(_waspService);
    }

    [Fact]
    public async Task ForceImport_EmitsManualImportEvent()
    {
      await Append(new ForceWaspImport("Operator requested"));

      var started = await Expect<ManualWaspImportEvent>();

      Assert.Equal("Operator requested", started.Trigger);
    }

    [Fact]
    public async Task ForceImport_EmitsClientBatchAndCompletesWhenHandled()
    {
      _waspService.Batches.Add(new WaspImportClientBatch("JOB-001", new List<string>
      {
        "JOB-001-Box-1",
        "JOB-001-Box 1-APP-41"
      }));

      await Append(new ForceWaspImport("Operator requested"));
      await ExpectManualImportStarted();

      var imported = await Expect<WaspClientAssetsImported>();

      Assert.Equal("JOB-001", imported.JobNumber);
      Assert.Collection(imported.Boxes, box =>
      {
        Assert.Equal("JOB-001-Box-1", box.AssetId);
        Assert.Equal("1", box.BoxName);
      });
      Assert.Collection(imported.Rolls, roll =>
      {
        Assert.Equal("JOB-001-Box 1-APP-41", roll.AssetId);
        Assert.Equal("1", roll.BoxName);
        Assert.Equal("APP-41", roll.RollName);
      });

      await Append(new WaspImportClientHandled("JOB-001"));

      await Expect<WaspImportCompleted>();
    }

    [Fact]
    public async Task ForceImport_EmitsIgnoredAssetsAlongsideAcceptedClientBatch()
    {
      _waspService.Batches.Add(new WaspImportClientBatch("JOB-001", new List<string>
      {
        "JOB-001-Box-1",
        "JOB-001-Legacy"
      }));

      await Append(new ForceWaspImport("Operator requested"));
      await ExpectManualImportStarted();

      var ignored = await Expect<WaspLegacyAssetsIgnored>();
      var ignoredAsset = Assert.Single(ignored.Assets);
      Assert.Equal("JOB-001-Legacy", ignoredAsset.AssetId);
      Assert.Contains("does not match the job-box or job-box-roll format", ignoredAsset.Reason);

      var imported = await Expect<WaspClientAssetsImported>();
      Assert.Equal("JOB-001", imported.JobNumber);
      Assert.Single(imported.Boxes);
      Assert.Empty(imported.Rolls);

      await Append(new WaspImportClientHandled("JOB-001"));

      await Expect<WaspImportCompleted>();
    }

    [Fact]
    public async Task ForceImport_AdvancesOnlyAfterClientHandled()
    {
      _waspService.Batches.Add(new WaspImportClientBatch("JOB-001", new List<string> { "JOB-001-Box-1" }));
      _waspService.Batches.Add(new WaspImportClientBatch("JOB-002", new List<string> { "JOB-002-Box-2" }));

      await Append(new ForceWaspImport("Operator requested"));
      await ExpectManualImportStarted();

      var first = await Expect<WaspClientAssetsImported>();
      Assert.Equal("JOB-001", first.JobNumber);

      var ex = await Assert.ThrowsAsync<ExpectException>(async () => await Expect<WaspClientAssetsImported>(200));
      Assert.IsType<TimeoutException>(ex.InnerException);

      await Append(new WaspImportClientHandled("JOB-001"));

      var second = await Expect<WaspClientAssetsImported>();
      Assert.Equal("JOB-002", second.JobNumber);

      await Append(new WaspImportClientHandled("JOB-002"));

      await Expect<WaspImportCompleted>();
    }

    [Fact]
    public async Task ForceImport_IgnoresUnbucketedAssetsWithoutAcceptedBatch()
    {
      _waspService.Batches.Add(new WaspImportClientBatch(null, new List<string> { "LEGACY-ASSET" }));

      await Append(new ForceWaspImport("Operator requested"));
      await ExpectManualImportStarted();

      var ignored = await Expect<WaspLegacyAssetsIgnored>();
      var ignoredAsset = Assert.Single(ignored.Assets);
      Assert.Equal("LEGACY-ASSET", ignoredAsset.AssetId);
      Assert.Contains("recognized client/job number prefix", ignoredAsset.Reason);

      await Expect<WaspImportClientHandled>();
      await Expect<WaspImportCompleted>();
    }

    [Fact]
    public async Task ForceImport_RunsWithoutSchedulingNextHourlyImport()
    {
      _waspService.Batches.Add(new WaspImportClientBatch("JOB-001", new List<string> { "JOB-001-Box-1" }));

      await Append(new WaspImportEnabledSet(true, "Enabled for test"));
      await Append(new ForceWaspImport("Operator requested"));
      await ExpectManualImportStarted();
      await Expect<WaspClientAssetsImported>();
      await Append(new WaspImportClientHandled("JOB-001"));
      await Expect<WaspImportCompleted>();

      var ex = await Assert.ThrowsAsync<ExpectException>(async () => await ExpectScheduled<HourlyWaspImportEvent>(200));
      Assert.IsType<TimeoutException>(ex.InnerException);
    }

    [Fact]
    public async Task HourlyImport_StillSchedulesNextRunWhenEnabled()
    {
      _waspService.Batches.Add(new WaspImportClientBatch("JOB-001", new List<string> { "JOB-001-Box-1" }));

      await Append(new WaspImportEnabledSet(true, "Enabled for test"));
      await Append(new HourlyWaspImportEvent(true, "Scheduled run"));

      await Expect<WaspImportStarted>();

      var scheduled = await ExpectScheduled<HourlyWaspImportEvent>();
      Assert.Equal("Scheduled recurring import", scheduled.Trigger);

      await Expect<WaspClientAssetsImported>();
      await Append(new WaspImportClientHandled("JOB-001"));
      await Expect<WaspImportCompleted>();
    }

    async Task ExpectManualImportStarted()
    {
      await Expect<ManualWaspImportEvent>();
      await Expect<WaspImportStarted>();
    }

    class FakeWaspAssetService : IWaspAssetService
    {
      public List<WaspImportClientBatch> Batches { get; } = new();

      public Task<WaspImportClientBatch> GetClientBatchAsync(int clientPosition)
      {
        if (clientPosition < 0 || clientPosition >= Batches.Count)
        {
          return Task.FromResult<WaspImportClientBatch>(null);
        }

        var batch = Batches[clientPosition];
        return Task.FromResult(new WaspImportClientBatch(batch.JobNumber, new List<string>(batch.AssetIds)));
      }
    }
  }

  public class WaspImportClientTopicTests : TopicTests<WaspClientImportTopic>
  {
    [Fact]
    public async Task ImportedBatch_ForKnownJobNumber_IsAcceptedForInternalClient()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new WaspClientAssetsImported(
        "JOB-001",
        new List<WaspAcceptedBoxAsset> { new("JOB-001-Box-1", "1") },
        new List<WaspAcceptedRollAsset> { new("JOB-001-Box 1-APP-41", "1", "APP-41") }));

      var accepted = await Expect<WaspClientAssetsAccepted>();

      Assert.Equal(clientId, accepted.ClientId);
      Assert.Equal("JOB-001", accepted.JobNumber);
      Assert.Single(accepted.Boxes);
      Assert.Single(accepted.Rolls);
    }

    [Fact]
    public async Task ImportedBatch_ForUnknownJobNumber_IsIgnoredAndHandled()
    {
      await Append(new WaspClientAssetsImported(
        "JOB-404",
        new List<WaspAcceptedBoxAsset> { new("JOB-404-Box-1", "1") },
        new List<WaspAcceptedRollAsset> { new("JOB-404-Box 1-APP-41", "1", "APP-41") }));

      var ignored = await Expect<WaspLegacyAssetsIgnored>();

      Assert.Collection(
        ignored.Assets,
        asset =>
        {
          Assert.Equal("JOB-404-Box-1", asset.AssetId);
          Assert.Contains("not registered to a known client", asset.Reason);
        },
        asset =>
        {
          Assert.Equal("JOB-404-Box 1-APP-41", asset.AssetId);
          Assert.Contains("not registered to a known client", asset.Reason);
        });

      var handled = await Expect<WaspImportClientHandled>();
      Assert.Equal("JOB-404", handled.JobNumber);
    }
  }

  public class WaspImportBoxManagerTopicTests : TopicTests<BoxManagerTopic>
  {
    [Fact]
    public async Task AcceptedBatch_CreatesMissingBoxes_EmitsPerBoxRollBatch_AndHandlesClient()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new WaspClientAssetsAccepted(
        clientId,
        "JOB-001",
        new List<WaspAcceptedBoxAsset> { new("JOB-001-Box-1", "1") },
        new List<WaspAcceptedRollAsset> { new("JOB-001-Box 1-APP-41", "1", "APP-41") }));

      var boxCreated = await Expect<BoxCreated>();
      Assert.Equal("1", boxCreated.Box.BoxName);
      Assert.Equal(clientId, boxCreated.Box.ClientId);

      var rollBatch = await Expect<WaspBoxRollsIdentified>();
      Assert.Equal("JOB-001", rollBatch.JobNumber);
      Assert.Equal(boxCreated.Box.BoxId, rollBatch.BoxId);
      Assert.Equal(clientId, rollBatch.ClientId);
      Assert.Collection(rollBatch.Rolls, roll =>
      {
        Assert.Equal("JOB-001-Box 1-APP-41", roll.AssetId);
        Assert.Equal("1", roll.BoxName);
        Assert.Equal("APP-41", roll.RollName);
      });

      var handled = await Expect<WaspImportClientHandled>();
      Assert.Equal("JOB-001", handled.JobNumber);
    }

    [Fact]
    public async Task AcceptedBatch_GroupsMultipleRollsIntoOneBatchPerBox()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new WaspClientAssetsAccepted(
        clientId,
        "JOB-001",
        new List<WaspAcceptedBoxAsset>
        {
          new("JOB-001-Box-1", "1"),
          new("JOB-001-Box-2", "2")
        },
        new List<WaspAcceptedRollAsset>
        {
          new("JOB-001-Box 1-APP-41", "1", "APP-41"),
          new("JOB-001-Box 1-APP-42", "1", "APP-42"),
          new("JOB-001-Box 2-APP-51", "2", "APP-51")
        }));

      var createdBoxes = new[]
      {
        await Expect<BoxCreated>(),
        await Expect<BoxCreated>()
      };
      var boxIdsByName = createdBoxes.ToDictionary(created => created.Box.BoxName, created => created.Box.BoxId);

      var batches = new[]
      {
        await Expect<WaspBoxRollsIdentified>(),
        await Expect<WaspBoxRollsIdentified>()
      }.ToDictionary(batch => batch.BoxId);

      Assert.Collection(batches[boxIdsByName["1"]].Rolls,
        roll =>
        {
          Assert.Equal("JOB-001-Box 1-APP-41", roll.AssetId);
          Assert.Equal("APP-41", roll.RollName);
        },
        roll =>
        {
          Assert.Equal("JOB-001-Box 1-APP-42", roll.AssetId);
          Assert.Equal("APP-42", roll.RollName);
        });

      Assert.Collection(batches[boxIdsByName["2"]].Rolls,
        roll =>
        {
          Assert.Equal("JOB-001-Box 2-APP-51", roll.AssetId);
          Assert.Equal("APP-51", roll.RollName);
        });

      var handled = await Expect<WaspImportClientHandled>();
      Assert.Equal("JOB-001", handled.JobNumber);
    }

    [Fact]
    public async Task AcceptedBatch_FailsClientWhenRollBoxIsMissing()
    {
      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new WaspClientAssetsAccepted(
        clientId,
        "JOB-001",
        new List<WaspAcceptedBoxAsset>(),
        new List<WaspAcceptedRollAsset> { new("JOB-001-Box 1-APP-41", "1", "APP-41") }));

      var failed = await Expect<WaspClientImportFailed>();
      Assert.Equal("JOB-001", failed.JobNumber);
      Assert.Contains("not recognized", failed.Error);

      var handled = await Expect<WaspImportClientHandled>();
      Assert.Equal("JOB-001", handled.JobNumber);
    }
  }

  public class WaspImportStatusQueryTests : QueryTests<WaspImportStatusQuery>
  {
    [Fact]
    public async Task ImportLifecycle_UpdatesRunningStatus()
    {
      await Append(new WaspImportStarted(0));

      var running = await GetQuery();

      Assert.True(running.IsRunning);

      await Append(new WaspImportCompleted());

      var completed = await GetQuery();

      Assert.False(completed.IsRunning);
    }

    [Fact]
    public async Task FailedImport_ClearsRunningStatus()
    {
      await Append(new WaspImportStarted(0));
      await Append(new WaspImportFailed("WASP unavailable", "AssetImport"));

      var query = await GetQuery();

      Assert.False(query.IsRunning);
    }

    [Fact]
    public async Task ManualImportEvent_ResetsLastRunStatus()
    {
      await Append(new WaspClientAssetsAccepted(
        Id.From("00000000-0000-0000-0000-000000000101"),
        "JOB-001",
        new List<WaspAcceptedBoxAsset> { new("JOB-001-Box-1", "1") },
        new List<WaspAcceptedRollAsset>()));
      await Append(new WaspLegacyAssetsIgnored(new List<IgnoredWaspLegacyAsset>
      {
        new("LEGACY-ASSET", "Unknown format")
      }));
      await Append(new WaspClientImportFailed("JOB-001", "Box missing"));
      await Append(new ManualWaspImportEvent("Operator requested"));

      var query = await GetQuery();

      Assert.Null(query.LastError);
      Assert.Null(query.LastFailureStep);
      Assert.Empty(query.LastImportedAssetIds);
      Assert.Equal(0, query.LastImportedAssetCount);
      Assert.Equal(0, query.LastIgnoredAssetCount);
    }

    [Fact]
    public async Task ClientBatchAccepted_UpdatesLastRunImportedCountsAndIds()
    {
      await Append(new WaspClientAssetsAccepted(
        Id.From("00000000-0000-0000-0000-000000000101"),
        "JOB-001",
        new List<WaspAcceptedBoxAsset> { new("JOB-001-Box-1", "1") },
        new List<WaspAcceptedRollAsset> { new("JOB-001-Box 1-APP-41", "1", "APP-41") }));

      var query = await GetQuery();

      Assert.Equal(2, query.LastImportedAssetCount);
      Assert.Contains("JOB-001-Box-1", query.LastImportedAssetIds);
      Assert.Contains("JOB-001-Box 1-APP-41", query.LastImportedAssetIds);
    }

    [Fact]
    public async Task CompletedImportWithEmptySummary_PreservesBatchAccumulatedStatus()
    {
      await Append(new ManualWaspImportEvent("Operator requested"));
      await Append(new WaspClientAssetsAccepted(
        Id.From("00000000-0000-0000-0000-000000000101"),
        "JOB-001",
        new List<WaspAcceptedBoxAsset> { new("JOB-001-Box-1", "1") },
        new List<WaspAcceptedRollAsset>()));
      await Append(new WaspClientImportFailed("JOB-001", "Box missing"));
      await Append(new WaspImportCompleted());

      var query = await GetQuery();

      Assert.Equal(1, query.LastImportedAssetCount);
      Assert.Contains("JOB-001-Box-1", query.LastImportedAssetIds);
      Assert.Equal("Box missing", query.LastError);
      Assert.Equal("ClientImport", query.LastFailureStep);
    }

    [Fact]
    public async Task WaspLegacyAssetsIgnored_AddsAllIgnoredAssetIdsAndLastRunCount()
    {
      await Append(new WaspLegacyAssetsIgnored(new List<IgnoredWaspLegacyAsset>
      {
        new("JOB-404-Box-1", "Unknown client"),
        new("LEGACY-ASSET", "Unknown format")
      }));

      var query = await GetQuery();

      Assert.Contains("JOB-404-Box-1", query.IgnoredLegacyAssetIds);
      Assert.Contains("LEGACY-ASSET", query.IgnoredLegacyAssetIds);
      Assert.Equal(2, query.LastIgnoredAssetCount);
    }
  }
}
