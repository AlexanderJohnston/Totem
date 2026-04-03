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

      await Expect<ManualWaspImportEvent>();

      var identified = await Expect<WaspBoxIdentified>();
      Assert.Equal("JOB-001-Box-1", identified.AssetId);

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

      await Expect<ManualWaspImportEvent>();

      var identified = await Expect<WaspRollIdentified>();
      Assert.Equal("JOB-001-Box 1-APP-41", identified.AssetId);
      Assert.Equal("JOB-001", identified.JobNumber);
      Assert.Equal("APP-41", identified.RollName);
      Assert.Equal(boxId, identified.BoxId);
      Assert.Equal(clientId, identified.ClientId);

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

      await Expect<ManualWaspImportEvent>();

      var identified = await Expect<WaspRollIdentified>();
      Assert.Equal("JOB-001-Box 1-Cartridge 1", identified.AssetId);
      Assert.Equal("Cartridge 1", identified.RollName);
      Assert.Equal(boxId, identified.BoxId);
      Assert.Equal(clientId, identified.ClientId);
    }

    [Fact]
    public async Task HourlyImport_StillSchedulesNextRunWhenEnabled()
    {
      _waspService.AssetIds.Add("JOB-001-Box-1");

      var clientId = Id.From("00000000-0000-0000-0000-000000000101");

      await Append(new ClientCreated(new KnownClient("Job 1", "JOB-001", clientId, Id.Unassigned)));
      await Append(new WaspImportEnabledSet(true, "Enabled for test"));
      await Append(new HourlyWaspImportEvent(true, "Scheduled run"));

      await Expect<WaspBoxIdentified>();
      await Expect<WaspImportCompleted>();

      var scheduled = await ExpectScheduled<HourlyWaspImportEvent>();
      Assert.Equal("Scheduled recurring import", scheduled.Trigger);
    }

    class FakeWaspAssetService : IWaspAssetService
    {
      public List<string> AssetIds { get; } = new();

      public Task<List<string>> GetAssetIdsAsync() =>
        Task.FromResult(new List<string>(AssetIds));
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
