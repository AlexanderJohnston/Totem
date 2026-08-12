using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Outermind.Microfilm;
using Outermind.Microfilm.Topics;
using Quantum.Web.Controllers;
using Quantum.Web.ScanProcessing;
using Xunit;
using Totem;
using Totem.App.Tests;

namespace Quantum.Tests
{
  public class ScanProcessingDemoTests
  {
    [Fact]
    public void ScanLifecycleAdvancesAndRepeatedRequestsAreIdempotent()
    {
      var (store, clock) = CreateStore();
      var discovery = store.Discover("actor-1", "roll-1", "row-1");
      var request = new StartScanDemoRequest
      {
        ExpectedResourceVersion = discovery.Context.ResourceVersion,
        ParentResourceRefId = discovery.Scan.Parent.Id,
        FolderName = "Roll_1",
        Notes = "demo notes",
        IdempotencyKey = "start-1"
      };

      var accepted = store.Start("actor-1", "roll-1", "row-1", request);
      var repeated = store.Start("actor-1", "roll-1", "row-1", request);

      Assert.True(accepted.Succeeded);
      Assert.Equal(202, accepted.StatusCode);
      Assert.Equal(RollScanState.Starting, accepted.Value.Context.ScanState);
      Assert.Equal(accepted.Value.Scan.ScanId, repeated.Value.Scan.ScanId);

      var mismatch = store.Start("actor-1", "roll-1", "row-1", new StartScanDemoRequest
      {
        ExpectedResourceVersion = request.ExpectedResourceVersion,
        ParentResourceRefId = request.ParentResourceRefId,
        FolderName = "Different",
        IdempotencyKey = request.IdempotencyKey
      });
      Assert.Equal(ScanProcessingDemoIssueCodes.IdempotencyKeyMismatch, mismatch.Issue.Code);

      clock.Advance(TimeSpan.FromSeconds(1));
      var active = store.Discover("actor-1", "roll-1", "row-1");
      Assert.Equal(RollScanState.Active, active.Context.ScanState);

      var finishing = store.Finish("actor-1", "roll-1", "row-1", accepted.Value.Scan.ScanId, new FinishScanDemoRequest
      {
        ExpectedResourceVersion = active.Context.ResourceVersion,
        Notes = "finished",
        IdempotencyKey = "finish-1"
      });
      Assert.Equal(RollScanState.Finishing, finishing.Value.Context.ScanState);

      clock.Advance(TimeSpan.FromSeconds(1));
      var idle = store.Discover("actor-1", "roll-1", "row-1");
      Assert.Equal(RollScanState.Idle, idle.Context.ScanState);
      Assert.Null(idle.Context.ActiveScanId);
      Assert.Contains(OperationAction.Process, idle.Context.EligibleActions);
    }

    [Fact]
    public void PreviewApplyAndPollingCompleteWithoutExternalResources()
    {
      var (store, clock) = CreateStore();
      var discovery = store.Discover("actor-1", "roll-2", "row-2");
      var preview = store.PreviewQpf("actor-1", "roll-2", "row-2", new PreviewQpfSettingsDemoRequest
      {
        ExpectedResourceVersion = discovery.Context.ResourceVersion,
        Settings = new Dictionary<string, string> { ["rotate"] = "90", ["contrast"] = "72" }
      });

      Assert.True(preview.Succeeded);
      Assert.Equal("qpf-settings", preview.Value.Purpose);
      Assert.True(preview.Value.ExpiresAt > clock.UtcNow);

      var applied = store.Apply("actor-1", preview.Value.PlanId, new ApplyScanProcessingDemoPlanRequest
      {
        ExpectedPlanVersion = preview.Value.PlanVersion,
        IdempotencyKey = "apply-1"
      });
      Assert.Equal(ScanProcessingDemoJobStatus.Queued, applied.Value.Status);

      var hiddenFromAnotherActor = store.GetJob("actor-2", applied.Value.JobId);
      Assert.Equal(404, hiddenFromAnotherActor.StatusCode);

      clock.Advance(TimeSpan.FromSeconds(1));
      Assert.Equal(ScanProcessingDemoJobStatus.Running, store.GetJob("actor-1", applied.Value.JobId).Value.Status);
      clock.Advance(TimeSpan.FromSeconds(1));
      Assert.Equal(ScanProcessingDemoJobStatus.Completed, store.GetJob("actor-1", applied.Value.JobId).Value.Status);

      var refreshed = store.Discover("actor-1", "roll-2", "row-2");
      Assert.Equal("90", refreshed.Processing.QpfSettings["rotate"]);
      Assert.Equal("72", refreshed.Processing.QpfSettings["contrast"]);
    }

    [Fact]
    public void StaleVersionsAndActorBoundResourceReferencesAreRejected()
    {
      var (store, _) = CreateStore();
      var discovery = store.Discover("actor-1", "roll-3", "row-3");

      var wrongActor = store.Start("actor-2", "roll-3", "row-3", new StartScanDemoRequest
      {
        ExpectedResourceVersion = discovery.Context.ResourceVersion,
        ParentResourceRefId = discovery.Scan.Parent.Id,
        FolderName = "Roll_3",
        IdempotencyKey = "wrong-actor"
      });
      Assert.Equal(ScanProcessingDemoIssueCodes.ResourceReferenceInvalid, wrongActor.Issue.Code);

      var stale = store.Start("actor-1", "roll-3", "row-3", new StartScanDemoRequest
      {
        ExpectedResourceVersion = "rv1-0000000000000000",
        ParentResourceRefId = discovery.Scan.Parent.Id,
        FolderName = "Roll_3",
        IdempotencyKey = "stale"
      });
      Assert.Equal(ScanProcessingDemoIssueCodes.ResourceVersionStale, stale.Issue.Code);

      var physicalFolder = store.Start("actor-1", "roll-3", "row-3", new StartScanDemoRequest
      {
        ExpectedResourceVersion = discovery.Context.ResourceVersion,
        ParentResourceRefId = discovery.Scan.Parent.Id,
        FolderName = "C:\\forged\\folder",
        IdempotencyKey = "physical-folder"
      });
      Assert.Equal(ScanProcessingDemoIssueCodes.InvalidRequest, physicalFolder.Issue.Code);

      var forgedFrames = store.PreviewFrames("actor-1", "roll-3", "row-3", new PreviewFramesPathsDemoRequest
      {
        ExpectedResourceVersion = discovery.Context.ResourceVersion,
        GrayscaleResourceRefId = "C:\\forged\\frames",
        BitonalResourceRefId = discovery.Processing.BitonalFrames.Id
      });
      Assert.Equal(ScanProcessingDemoIssueCodes.ResourceReferenceInvalid, forgedFrames.Issue.Code);
    }

    [Fact]
    public void RequestContractsContainNoActorServerOrPhysicalPathAuthority()
    {
      var requestTypes = new[]
      {
        typeof(StartScanDemoRequest),
        typeof(FinishScanDemoRequest),
        typeof(PreviewQpfSettingsDemoRequest),
        typeof(PreviewFramesPathsDemoRequest),
        typeof(ApplyScanProcessingDemoPlanRequest)
      };

      var forbiddenNames = new[] { "actor", "role", "permission", "server", "client", "workspace", "path", "root", "fullPath" };
      foreach(var type in requestTypes)
      {
        var propertyNames = type.GetProperties().Select(property => property.Name).ToList();
        Assert.DoesNotContain(propertyNames, name => forbiddenNames.Any(forbidden =>
          name.Equals(forbidden, StringComparison.OrdinalIgnoreCase)
          || name.EndsWith(forbidden, StringComparison.OrdinalIgnoreCase)));
      }

      var (store, _) = CreateStore();
      var discovery = store.Discover("actor-1", "roll-4", "row-4");
      Assert.StartsWith("demo-ref-", discovery.Scan.Parent.Id);
      Assert.StartsWith("demo://", discovery.Scan.Parent.InformationalFullPath);
      Assert.DoesNotContain("\\\\", JsonSerializer.Serialize(discovery));
    }

    [Fact]
    public void ControllerExposesTheCompleteDemoRouteSetAndMarksResponses()
    {
      var routes = typeof(ScanProcessingDemoController)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
        .Select(attribute => $"{attribute.HttpMethods.Single()} {attribute.Template}")
        .OrderBy(route => route)
        .ToList();

      Assert.Equal(9, routes.Count);
      Assert.Contains("GET rolls/{rollId}/rows/{rowId}/discovery", routes);
      Assert.Contains("POST demo/reset", routes);
      Assert.Contains("POST plans/{planId}/apply", routes);

      var options = Options.Create(EnabledOptions());
      var controller = new ScanProcessingDemoController(
        new ScanProcessingDemoStore(options, new TestClock()),
        options,
        new RegisteredScanProcessingActorResolver(),
        new ScanProcessingDemoPhysicalWorkflow(options),
        null,
        null)
      {
        ControllerContext = new ControllerContext
        {
          HttpContext = new DefaultHttpContext
          {
            User = new ClaimsPrincipal(new ClaimsIdentity())
          }
        }
      };

      Assert.IsType<OkObjectResult>(controller.Discover("roll-5", "row-5"));
      Assert.Equal("true", controller.Response.Headers["X-Scan-Processing-Demo"]);
      Assert.Equal("no-store", controller.Response.Headers.CacheControl);
    }

    [Fact]
    public void ResetClearsAllEphemeralState()
    {
      var (store, _) = CreateStore();
      var discovery = store.Discover("actor-1", "roll-reset", "row-reset");
      store.Start("actor-1", "roll-reset", "row-reset", new StartScanDemoRequest
      {
        ExpectedResourceVersion = discovery.Context.ResourceVersion,
        ParentResourceRefId = discovery.Scan.Parent.Id,
        FolderName = "before-reset",
        IdempotencyKey = "before-reset"
      });

      store.Reset();
      var reset = store.Discover("actor-1", "roll-reset", "row-reset");
      Assert.Equal(RollScanState.Idle, reset.Context.ScanState);
      Assert.Equal(RollResourceVersion.FromRevision(1), reset.Context.ResourceVersion);
    }

    [Fact]
    public void FrontendSurfaceFixtureIsValidJsonAndMarkedDisposable()
    {
      var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ScanProcessing", "demo.frontend-surface.json");
      using var fixture = JsonDocument.Parse(File.ReadAllText(path));

      Assert.Equal("scan-processing-demo-frontend-surface", fixture.RootElement.GetProperty("contract").GetString());
      Assert.True(fixture.RootElement.GetProperty("disposable").GetBoolean());
      Assert.Equal("demo://", fixture.RootElement
        .GetProperty("discovery")
        .GetProperty("scan")
        .GetProperty("parent")
        .GetProperty("informationalFullPath")
        .GetString()
        ?.Substring(0, 7));
    }

    [Fact]
    public void PhysicalWorkflowCreatesOneScopedFolderAndCountsIdfFramesLikeQuantumProcess()
    {
      var root = NewTempRoot();
      try
      {
        var workflow = PhysicalWorkflow(root);
        var folder = workflow.CreateScanFolder("Roll_100");
        Assert.True(folder.Succeeded);
        Assert.Equal(root, Directory.GetParent(folder.Value)?.FullName);

        File.WriteAllText(Path.Combine(folder.Value, "Roll_100.idf"), """
          <?xml version="1.0" encoding="utf-8"?>
          <Roll>
            <ScannerSettings>
              <DetectionSettings BlipDetection="1">
                <Frame LeadEdge="1" IsBlip="False" />
                <Frame LeadEdge="2" IsBlip="True" />
              </DetectionSettings>
              <DetectionSettings BlipDetection="0">
                <Frame LeadEdge="3" IsBlip="True" />
                <Frame LeadEdge="4" />
              </DetectionSettings>
            </ScannerSettings>
          </Roll>
          """);

        var inspected = workflow.InspectIdf(folder.Value);
        Assert.True(inspected.Succeeded);
        Assert.Equal(3, inspected.Value.ImageCount);
        Assert.Equal("Roll_100.idf", inspected.Value.IdfFileName);
      }
      finally
      {
        Directory.Delete(root, true);
      }
    }

    [Fact]
    public void PhysicalQpfApplyPreservesUnknownXmlAndCreatesVerifiedBackup()
    {
      var root = NewTempRoot();
      try
      {
        var workflow = PhysicalWorkflow(root);
        var folder = workflow.CreateScanFolder("Roll_101").Value;
        var qpfPath = Path.Combine(folder, "Roll_101.qpf");
        var original = """
          <?xml version="1.0" encoding="utf-8"?>
          <Roll Unknown="preserve-me">
            <ScannerSettings ID="1">
              <DetectionSettings ID="1" ProcessSettingsExist="1" ProcessSoftwareContrast="64" ProcessCrop="0" ProcessRotate="0">
                <Frame LeadEdge="1" />
              </DetectionSettings>
            </ScannerSettings>
            <ScannerSettings ID="2">
              <DetectionSettings ID="2" ProcessSettingsExist="0" ProcessSoftwareContrast="64" ProcessCrop="0" ProcessRotate="0" />
            </ScannerSettings>
          </Roll>
          """;
        File.WriteAllText(qpfPath, original);

        var applied = workflow.ApplyQpfSettings(new ScanProcessingDemoPlanExecution(
          "qpf-settings",
          "roll-101",
          "row-101",
          folder,
          new Dictionary<string, string>
          {
            ["contrast"] = "72",
            ["auto-crop"] = "true",
            ["rotate"] = "90"
          }));

        Assert.True(applied.Succeeded);
        Assert.Equal(2, applied.Value.UpdatedDetectionSettings);
        Assert.Equal(original, File.ReadAllText(Path.Combine(folder, applied.Value.BackupFileName)));

        var updated = System.Xml.Linq.XDocument.Load(qpfPath);
        Assert.Equal("preserve-me", updated.Root?.Attribute("Unknown")?.Value);
        var targets = updated.Root?.Elements("ScannerSettings").SelectMany(node => node.Elements("DetectionSettings")).ToList();
        Assert.All(targets, target =>
        {
          Assert.Equal("1", target.Attribute("ProcessSettingsExist")?.Value);
          Assert.Equal("72", target.Attribute("ProcessSoftwareContrast")?.Value);
          Assert.Equal("1", target.Attribute("ProcessCrop")?.Value);
          Assert.Equal("90", target.Attribute("ProcessRotate")?.Value);
        });
        Assert.Single(targets[0].Elements("Frame"));
      }
      finally
      {
        Directory.Delete(root, true);
      }
    }

    [Fact]
    public void PhysicalWorkflowRejectsFolderCollisionsAndAmbiguousIdfFiles()
    {
      var root = NewTempRoot();
      try
      {
        var workflow = PhysicalWorkflow(root);
        var folder = workflow.CreateScanFolder("Roll_102");
        Assert.Equal(ScanProcessingDemoIssueCodes.ScanFolderCollision, workflow.CreateScanFolder("Roll_102").Issue.Code);

        File.WriteAllText(Path.Combine(folder.Value, "one.idf"), "<Roll />");
        File.WriteAllText(Path.Combine(folder.Value, "two.IDF"), "<Roll />");
        Assert.Equal(ScanProcessingDemoIssueCodes.IdfAmbiguous, workflow.InspectIdf(folder.Value).Issue.Code);
      }
      finally
      {
        Directory.Delete(root, true);
      }
    }

    [Fact]
    public void PhysicalScanFolderFlowsIntoSynchronousQpfPlanApply()
    {
      var root = NewTempRoot();
      try
      {
        var optionsValue = EnabledOptions();
        optionsValue.PhysicalWorkflowEnabled = true;
        optionsValue.PhysicalScanRoot = root;
        var options = Options.Create(optionsValue);
        var clock = new TestClock();
        var store = new ScanProcessingDemoStore(options, clock);
        var workflow = new ScanProcessingDemoPhysicalWorkflow(options);

        var discovery = store.Discover("actor-physical", "roll-physical", "row-physical");
        var started = store.Start("actor-physical", "roll-physical", "row-physical", new StartScanDemoRequest
        {
          ExpectedResourceVersion = discovery.Context.ResourceVersion,
          ParentResourceRefId = discovery.Scan.Parent.Id,
          FolderName = "Roll_Physical",
          IdempotencyKey = "physical-start"
        });
        var folder = workflow.CreateScanFolder(started.Value.Scan.FolderName).Value;
        store.AttachScanFolder("actor-physical", "roll-physical", "row-physical", started.Value.Scan.ScanId, folder);

        File.WriteAllText(Path.Combine(folder, "Roll_Physical.qpf"), """
          <?xml version="1.0" encoding="utf-8"?>
          <Roll><ScannerSettings><DetectionSettings ProcessSettingsExist="1" ProcessSoftwareContrast="64" /></ScannerSettings></Roll>
          """);

        clock.Advance(TimeSpan.FromSeconds(1));
        var active = store.Discover("actor-physical", "roll-physical", "row-physical");
        var finished = store.Finish("actor-physical", "roll-physical", "row-physical", started.Value.Scan.ScanId, new FinishScanDemoRequest
        {
          ExpectedResourceVersion = active.Context.ResourceVersion,
          IdempotencyKey = "physical-finish"
        });
        Assert.True(finished.Succeeded);

        clock.Advance(TimeSpan.FromSeconds(1));
        var idle = store.Discover("actor-physical", "roll-physical", "row-physical");
        var preview = store.PreviewQpf("actor-physical", "roll-physical", "row-physical", new PreviewQpfSettingsDemoRequest
        {
          ExpectedResourceVersion = idle.Context.ResourceVersion,
          Settings = new Dictionary<string, string> { ["contrast"] = "88" }
        });
        var applied = store.Apply("actor-physical", preview.Value.PlanId, new ApplyScanProcessingDemoPlanRequest
        {
          ExpectedPlanVersion = preview.Value.PlanVersion,
          IdempotencyKey = "physical-apply"
        }, workflow.ApplyQpfSettings);

        Assert.True(applied.Succeeded);
        Assert.Equal(ScanProcessingDemoJobStatus.Completed, applied.Value.Status);
        Assert.Equal(1, applied.Value.QpfApply.UpdatedDetectionSettings);
        Assert.Equal("88", System.Xml.Linq.XDocument.Load(Path.Combine(folder, "Roll_Physical.qpf"))
          .Root?.Element("ScannerSettings")?.Element("DetectionSettings")?.Attribute("ProcessSoftwareContrast")?.Value);
      }
      finally
      {
        Directory.Delete(root, true);
      }
    }

    [Fact]
    public void PhysicalFinishResultCanBeReplayedByItsSemanticIdempotencyKey()
    {
      var (store, _) = CreateStore();
      var request = new FinishScanDemoRequest
      {
        ExpectedResourceVersion = "rv1-0000000000000003",
        Notes = "finished",
        IdempotencyKey = "finish-replay"
      };
      var response = new FinishScanProcessingDemoResponse(
        new OperationRowContext { RollId = "roll-replay", RowId = "row-replay" },
        new ScanProcessingDemoScan(
          "scan-replay",
          "finishing",
          "Roll_Replay",
          "finished",
          DateTimeOffset.UtcNow,
          "C:\\demo\\Roll_Replay"),
        123,
        "Roll_Replay.idf");

      store.RememberPhysicalFinish("actor-replay", "scan-replay", request, response);

      Assert.True(store.TryReplayPhysicalFinish("actor-replay", "scan-replay", request, out var replay));
      Assert.Equal(123, replay.ImageCount);
      Assert.False(store.TryReplayPhysicalFinish("other-actor", "scan-replay", request, out _));
      Assert.False(store.TryReplayPhysicalFinish("actor-replay", "other-scan", request, out _));
    }

    static (ScanProcessingDemoStore Store, TestClock Clock) CreateStore()
    {
      var clock = new TestClock();
      return (new ScanProcessingDemoStore(Options.Create(EnabledOptions()), clock), clock);
    }

    static ScanProcessingDemoOptions EnabledOptions() =>
      new()
      {
        Enabled = true,
        AllowSyntheticActor = true,
        TransitionDelayMilliseconds = 100,
        PlanLifetimeMinutes = 30
      };

    static string NewTempRoot()
    {
      var root = Path.Combine(Path.GetTempPath(), $"totem-scan-demo-tests-{Guid.NewGuid():N}");
      Directory.CreateDirectory(root);
      return root;
    }

    static ScanProcessingDemoPhysicalWorkflow PhysicalWorkflow(string root) =>
      new(Options.Create(new ScanProcessingDemoOptions
      {
        Enabled = true,
        PhysicalWorkflowEnabled = true,
        PhysicalScanRoot = root
      }));

    sealed class TestClock : IScanProcessingDemoClock
    {
      public DateTimeOffset UtcNow { get; private set; } = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

      public void Advance(TimeSpan duration) => UtcNow += duration;
    }
  }

  public sealed class ScanProcessingDemoDurableImageCountTests : TopicTests<RollMicrofilmTableTopic>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000111");
    static readonly Id BoxId = Id.From("00000000-0000-0000-0000-000000000311");
    static readonly Id RollId = Id.From("00000000-0000-0000-0000-000000000411");

    [Fact]
    public async System.Threading.Tasks.Task FinishImageCountIsADurableNumericRowCellFact()
    {
      await Append(new RollCreated(new KnownRoll("Demo Roll", RollId, BoxId)));
      await Append(new CreateRollMicrofilmRow(
        RollId,
        ClientId,
        "row-image-count",
        MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue>(),
        Actor()));
      await Expect<RollMicrofilmRowCreated>();

      await Append(new UpdateRollMicrofilmRowCell(
        RollId,
        ClientId,
        "row-image-count",
        MicrofilmTableRowOrigins.Regular,
        "imageCount",
        MicrofilmCellValue.FromNumber(3723),
        Actor()));

      var changed = await Expect<RollMicrofilmRowCellChanged>();
      Assert.Equal("imageCount", changed.ColumnId);
      Assert.Equal(MicrofilmCellValueKind.Number, changed.Value.Kind);
      Assert.Equal(3723, changed.Value.Number);
      Assert.Equal("scan-processing-demo", changed.Actor.TrackingSource);
    }

    static MicrofilmAuditActorStamp Actor() =>
      new("identified", "demo-frontend", null, "scan-processing-demo");
  }
}
