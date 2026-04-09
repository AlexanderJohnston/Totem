using System;
using System.Collections.Generic;
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
    readonly FakeWaspAssetService _waspService = new FakeWaspAssetService();

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
    public async Task ForceImport_RunsImportWithoutSchedulingNextHourlyImport()
    {
      _waspService.AssetIds.Add("JOB-001-Box-1");

      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new WaspImportEnabledSet(true, "Enabled for test"));
      await Append(new ForceWaspImport("Operator requested"));

      await ExpectManualImportStarted();

      var identified = await Expect<WaspBoxIdentified>();
      Assert.Equal("JOB-001-Box-1", identified.AssetId);

      await Append(new BoxCreated(new KnownBox("Box 1", Id.From("00000000-0000-0000-0000-000000000201"), clientId)));

      var completed = await Expect<WaspImportCompleted>();
      Assert.Equal(1, completed.ImportedAssetCount);
      var ex = await Assert.ThrowsAsync<ExpectException>(async () => await ExpectScheduled<HourlyWaspImportEvent>(200));
      Assert.IsType<TimeoutException>(ex.InnerException);
    }

    [Fact]
    public async Task ForceImport_RecognizesRollsWithoutCartridgeInTheName()
    {
      _waspService.AssetIds.Add("JOB-001-Box 1-APP-41");

      var clientId = Id.From("00000000-0000-0000-0000-000000000101");
      var boxId = Id.From("00000000-0000-0000-0000-000000000201");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new BoxCreated(new KnownBox("Box 1", boxId, clientId)));
      await Append(new ForceWaspImport("Operator requested"));

      await ExpectManualImportStarted();

      var identified = await Expect<WaspRollIdentified>();
      Assert.Equal("JOB-001-Box 1-APP-41", identified.AssetId);
      Assert.Equal("JOB-001", identified.JobNumber);
      Assert.Equal("APP-41", identified.RollName);
      Assert.Equal(boxId, identified.BoxId);
      Assert.Equal(clientId, identified.ClientId);

      await Append(new RollCreated(new KnownRoll("APP-41", Id.From("00000000-0000-0000-0000-000000000301"), boxId)));

      var completed = await Expect<WaspImportCompleted>();
      Assert.Equal(1, completed.ImportedAssetCount);
      Assert.Equal(0, completed.IgnoredAssetCount);
    }

    [Fact]
    public async Task ForceImport_StillRecognizesCartridgeRolls()
    {
      _waspService.AssetIds.Add("JOB-001-Box 1-Cartridge 1");

      var clientId = Id.From("00000000-0000-0000-0000-000000000101");
      var boxId = Id.From("00000000-0000-0000-0000-000000000201");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new BoxCreated(new KnownBox("Box 1", boxId, clientId)));
      await Append(new ForceWaspImport("Operator requested"));

      await ExpectManualImportStarted();

      var identified = await Expect<WaspRollIdentified>();
      Assert.Equal("JOB-001-Box 1-Cartridge 1", identified.AssetId);
      Assert.Equal("Cartridge 1", identified.RollName);
      Assert.Equal(boxId, identified.BoxId);
      Assert.Equal(clientId, identified.ClientId);

      await Append(new RollCreated(new KnownRoll("Cartridge 1", Id.From("00000000-0000-0000-0000-000000000301"), boxId)));
      await Expect<WaspImportCompleted>();
    }

    [Fact]
    public async Task ForceImport_RecognizesRollWhenBoxAppearsLaterInSameBatch()
    {
      _waspService.AssetIds.Add("JOB-001-Box 1-APP-41");
      _waspService.AssetIds.Add("JOB-001-Box-1");

      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new ForceWaspImport("Operator requested"));

      await ExpectManualImportStarted();

      await Expect<WaspImportAssetRequeued>();

      var boxIdentified = await Expect<WaspBoxIdentified>();
      Assert.Equal("JOB-001-Box-1", boxIdentified.AssetId);

      var boxId = Id.From("00000000-0000-0000-0000-000000000201");
      await Append(new BoxCreated(new KnownBox("Box 1", boxId, clientId)));

      var rollIdentified = await Expect<WaspRollIdentified>();
      Assert.Equal("JOB-001-Box 1-APP-41", rollIdentified.AssetId);
      Assert.Equal("APP-41", rollIdentified.RollName);
      Assert.Equal(boxId, rollIdentified.BoxId);

      await Append(new RollCreated(new KnownRoll("APP-41", Id.From("00000000-0000-0000-0000-000000000301"), boxId)));

      var completed = await Expect<WaspImportCompleted>();
      Assert.Equal(2, completed.ImportedAssetCount);
      Assert.Equal(0, completed.DeferredAssetCount);
      Assert.Equal(0, completed.IgnoredAssetCount);
    }

    [Fact]
    public async Task ForceImport_ReimportsAssetsWithoutUsingAlreadyImportedState()
    {
      _waspService.AssetIds.Add("JOB-001-Box-1");

      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new ForceWaspImport("First import"));

      await ExpectManualImportStarted();
      await Expect<WaspBoxIdentified>();
      await Append(new BoxCreated(new KnownBox("Box 1", Id.From("00000000-0000-0000-0000-000000000201"), clientId)));
      await Expect<WaspImportCompleted>();

      await Append(new ForceWaspImport("Second import"));

      await ExpectManualImportStarted();

      var identifiedAgain = await Expect<WaspBoxIdentified>();
      Assert.Equal("JOB-001-Box-1", identifiedAgain.AssetId);

      await Append(new BoxAlreadyExists("Box 1", clientId));

      var completedAgain = await Expect<WaspImportCompleted>();
      Assert.Equal(1, completedAgain.ImportedAssetCount);

      var ex = await Assert.ThrowsAsync<ExpectException>(async () => await Expect<WaspAssetAlreadyImported>(200));
      Assert.IsType<TimeoutException>(ex.InnerException);
    }

    [Fact]
    public async Task ForceImport_IgnoresAssetsForUnknownClients()
    {
      _waspService.AssetIds.Add("JOB-404-Box-1");

      await Append(new ForceWaspImport("Operator requested"));

      await ExpectManualImportStarted();

      var ignored = await Expect<WaspLegacyAssetIgnored>();
      Assert.Equal("JOB-404-Box-1", ignored.AssetId);
      Assert.Contains("not registered to a known client", ignored.Reason);

      var completed = await Expect<WaspImportCompleted>();
      Assert.Equal(0, completed.ImportedAssetCount);
      Assert.Equal(0, completed.DeferredAssetCount);
      Assert.Equal(1, completed.IgnoredAssetCount);
    }

    [Fact]
    public async Task ForceImport_DefersRollWhenBoxNeverBecomesKnown()
    {
      _waspService.AssetIds.Add("JOB-001-Box 1-APP-41");

      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new ForceWaspImport("Operator requested"));

      await ExpectManualImportStarted();

      await Expect<WaspImportAssetRequeued>();

      var deferred = await Expect<WaspAssetDeferred>();
      Assert.Equal("JOB-001-Box 1-APP-41", deferred.Asset.AssetId);
      Assert.Equal("JOB-001", deferred.Asset.JobNumber);
      Assert.Equal("Box 1", deferred.Asset.BoxName);
      Assert.Equal("APP-41", deferred.Asset.RollName);

      var completed = await Expect<WaspImportCompleted>();
      Assert.Equal(0, completed.ImportedAssetCount);
      Assert.Equal(1, completed.DeferredAssetCount);
      Assert.Equal(0, completed.IgnoredAssetCount);
    }

    [Fact]
    public async Task HourlyImport_StillSchedulesNextRunWhenEnabled()
    {
      _waspService.AssetIds.Add("JOB-001-Box-1");

      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new WaspImportEnabledSet(true, "Enabled for test"));
      await Append(new HourlyWaspImportEvent(true, "Scheduled run"));

      await Expect<WaspImportBatchLoaded>();
      var scheduled = await ExpectScheduled<HourlyWaspImportEvent>();
      Assert.Equal("Scheduled recurring import", scheduled.Trigger);
      await Expect<WaspBoxIdentified>();
      await Append(new BoxCreated(new KnownBox("Box 1", Id.From("00000000-0000-0000-0000-000000000201"), clientId)));
      await Expect<WaspImportCompleted>();
    }

    class FakeWaspAssetService : IWaspAssetService
    {
      public List<string> AssetIds { get; } = new();

      public Task<List<string>> GetAssetIdsAsync() =>
        Task.FromResult(new List<string>(AssetIds));
    }

    async Task ExpectManualImportStarted()
    {
      await Expect<ManualWaspImportEvent>();
      await Expect<WaspImportBatchLoaded>();
    }
  }

  public class WaspImportStatusQueryTests : QueryTests<WaspImportStatusQuery>
  {
    [Fact]
    public async Task ManualImportEvent_ResetsLastRunStatus()
    {
      await Append(new WaspImportFailed("Error", "AssetImport"));
      await Append(new WaspBoxIdentified("JOB-001-Box-1", "JOB-001", "Box 1", Id.From("00000000-0000-0000-0000-000000000101")));
      await Append(new ManualWaspImportEvent("Operator requested"));

      var query = await GetQuery();

      Assert.Null(query.LastError);
      Assert.Null(query.LastFailureStep);
      Assert.Empty(query.LastImportedAssetIds);
    }

    [Fact]
    public async Task ManualImportEvent_DoesNotChangeImportEnabled()
    {
      await Append(new WaspImportEnabledSet(true, "Enabled for test"));
      await Append(new ManualWaspImportEvent("Operator requested"));

      var query = await GetQuery();

      Assert.True(query.ImportEnabled);
    }
  }
}
