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
using Quantum.Web.Controllers;
using Quantum.Web.ScanProcessing;
using Xunit;

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
        Settings = new Dictionary<string, string> { ["rotation"] = "90", ["polarity"] = "negative" }
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
      Assert.Equal("90", refreshed.Processing.QpfSettings["rotation"]);
      Assert.Equal("negative", refreshed.Processing.QpfSettings["polarity"]);
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
        new RegisteredScanProcessingActorResolver())
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

    sealed class TestClock : IScanProcessingDemoClock
    {
      public DateTimeOffset UtcNow { get; private set; } = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

      public void Advance(TimeSpan duration) => UtcNow += duration;
    }
  }
}
