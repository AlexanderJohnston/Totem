using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Totem;
using Totem.App.Tests;
using Xunit;

namespace Quantum.Tests
{
  public class RollOperationQueryTests : QueryTests<RollOperationQuery>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");
    static readonly Id RollId = Id.From("00000000-0000-0000-0000-000000000401");

    [Fact]
    public async Task RollFacts_ProjectDurableBusinessVersionIndependentlyOfCells()
    {
      await Append(RollCreated());
      await Append(new RollMicrofilmRowCreated(
        RollId,
        ClientId,
        new MicrofilmTableRow("row-1", RollId.ToString(), MicrofilmTableRowOrigins.Custom, new Dictionary<string, MicrofilmCellValue>
        {
          ["isScanning"] = MicrofilmCellValue.FromCheckbox(true),
          ["rollName"] = MicrofilmCellValue.FromText("Wrong presentation name")
        }),
        Actor()));
      await Append(new RollMicrofilmRowCellChanged(
        RollId,
        ClientId,
        "row-1",
        MicrofilmTableRowOrigins.Custom,
        "isScanning",
        MicrofilmCellValue.FromCheckbox(false),
        Actor()));

      var query = await GetQuery(RollId);

      Assert.Equal(RollScanState.Idle, query.ScanState);
      Assert.False(query.IsScanning);
      Assert.Null(query.ActiveScanId);
      Assert.Equal(3, query.ResourceRevision);
      Assert.Equal("rv1-0000000000000003", query.ResourceVersion);
    }

    [Fact]
    public async Task DurableScanFacts_ProjectEveryTransitionalState()
    {
      await Append(RollCreated());

      await Append(new ScanStartAccepted(RollId, "row-1", "scan-1"));
      var starting = await GetQuery(RollId);
      Assert.Equal(RollScanState.Starting, starting.ScanState);
      Assert.True(starting.IsScanning);
      Assert.Equal("scan-1", starting.ActiveScanId);

      await Append(new ScanStarted(RollId, "row-1", "scan-1"));
      var active = await GetQuery(RollId);
      Assert.Equal(RollScanState.Active, active.ScanState);
      Assert.True(active.IsScanning);

      await Append(new ScanFinishAccepted(RollId, "row-1", "scan-1"));
      var finishing = await GetQuery(RollId);
      Assert.Equal(RollScanState.Finishing, finishing.ScanState);
      Assert.True(finishing.IsScanning);

      await Append(new ScanFinished(RollId, "row-1", "scan-1"));
      var finished = await GetQuery(RollId);
      Assert.Equal(RollScanState.Idle, finished.ScanState);
      Assert.False(finished.IsScanning);
      Assert.Null(finished.ActiveScanId);
      Assert.Equal(5, finished.ResourceRevision);
      Assert.Equal("rv1-0000000000000005", finished.ResourceVersion);
    }

    [Fact]
    public async Task FailedAcceptedStart_ReturnsDurableStateToIdle()
    {
      await Append(RollCreated());
      await Append(new ScanStartAccepted(RollId, "row-1", "scan-1"));
      await Append(new ScanStartFailed(RollId, "row-1", "scan-1"));

      var query = await GetQuery(RollId);

      Assert.Equal(RollScanState.Idle, query.ScanState);
      Assert.False(query.IsScanning);
      Assert.Null(query.ActiveScanId);
      Assert.Equal("rv1-0000000000000003", query.ResourceVersion);
    }

    static RollCreated RollCreated() =>
      new(new KnownRoll("Roll A", RollId, Id.From("00000000-0000-0000-0000-000000000301")));

    static MicrofilmAuditActorStamp Actor() =>
      new("identified", "Alex Johnston", "AJOHNSTON", "backend-cookie");
  }

  public class OperationContextHttpContractTests
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000101");
    static readonly Id BoxId = Id.From("00000000-0000-0000-0000-000000000301");
    static readonly Id RollId = Id.From("00000000-0000-0000-0000-000000000401");

    [Theory]
    [InlineData(MicrofilmTableRowOrigins.Regular, "operation-context.regular.json", RollScanState.Idle, null, "rv1-0000000000000002")]
    [InlineData(MicrofilmTableRowOrigins.Custom, "operation-context.custom.json", RollScanState.Active, "scan-0001", "rv1-0000000000000004")]
    public void ContractFixture_PreservesOriginParityAndTypedState(
      string origin,
      string fixtureName,
      RollScanState scanState,
      string activeScanId,
      string resourceVersion)
    {
      var context = OperationRowContextFactory.Create(
        new KnownClient("Example job", "JOB-001", ClientId, Id.From("00000000-0000-0000-0000-000000000001")),
        new KnownRoll("Roll A", RollId, BoxId),
        new MicrofilmTableRow("row-1", RollId.ToString(), origin, new Dictionary<string, MicrofilmCellValue>()),
        scanState,
        activeScanId,
        resourceVersion);

      var json = JsonSerializer.Serialize(context, SerializerOptions());
      var document = JsonNode.Parse(json);

      Assert.True(JsonNode.DeepEquals(ReadFixture(fixtureName), document));
      Assert.Equal(
        scanState == RollScanState.Idle ? JsonValueKind.False : JsonValueKind.True,
        document!["isScanning"]!.GetValueKind());
      Assert.Equal(scanState.ToString().ToLowerInvariant(), document["scanState"]!.GetValue<string>());
      Assert.Equal("scan", document["eligibleActions"]![0]!.GetValue<string>());

      if(scanState == RollScanState.Idle)
      {
        Assert.Equal("process", document["eligibleActions"]![1]!.GetValue<string>());
      }
      else
      {
        Assert.Single(document["eligibleActions"]!.AsArray());
      }
    }

    [Theory]
    [InlineData(RollScanState.Idle)]
    [InlineData(RollScanState.Starting)]
    [InlineData(RollScanState.Active)]
    [InlineData(RollScanState.Finishing)]
    public void Eligibility_IsIdenticalForRegularAndCustomOrigins(RollScanState scanState)
    {
      var regular = CreateContext(MicrofilmTableRowOrigins.Regular, scanState);
      var custom = CreateContext(MicrofilmTableRowOrigins.Custom, scanState);

      Assert.Equal(regular.EligibleActions, custom.EligibleActions);
      Assert.Equal(regular.IsScanning, custom.IsScanning);
    }

    [Fact]
    public void Context_UsesCanonicalRollAndDurableStateInsteadOfVisibleCells()
    {
      var row = new MicrofilmTableRow(
        "row-1",
        RollId.ToString(),
        MicrofilmTableRowOrigins.Regular,
        new Dictionary<string, MicrofilmCellValue>
        {
          ["rollName"] = MicrofilmCellValue.FromText("Presentation-only name"),
          ["isScanning"] = MicrofilmCellValue.FromCheckbox(false)
        });

      var context = OperationRowContextFactory.Create(
        new KnownClient("Example job", "JOB-001", ClientId, Id.From("00000000-0000-0000-0000-000000000001")),
        new KnownRoll("Canonical Roll A", RollId, BoxId),
        row,
        RollScanState.Active,
        "scan-0001",
        "rv1-0000000000000004");

      Assert.Equal("Canonical Roll A", context.RollName);
      Assert.True(context.IsScanning);
      Assert.Equal(RollScanState.Active, context.ScanState);
    }

    [Fact]
    public void StructuredIssueContract_CoversRequiredFoundationCodes()
    {
      var envelopes = new[]
      {
        OperationContextApiErrors.RollNotFound("roll-1"),
        OperationContextApiErrors.RowNotFound("roll-1", "row-1"),
        OperationContextApiErrors.RollMappingInvalid("roll-1"),
        OperationContextApiErrors.RowContextInvalid("roll-1", "row-1"),
        OperationContextApiErrors.ResourceVersionStale(),
        OperationContextApiErrors.ActionIneligible(OperationAction.Process)
      };

      Assert.Collection(envelopes,
        item => Assert.Equal("ROLL_NOT_FOUND", item.Issues[0].Code),
        item => Assert.Equal("ROW_NOT_FOUND", item.Issues[0].Code),
        item => Assert.Equal("ROLL_MAPPING_INVALID", item.Issues[0].Code),
        item => Assert.Equal("ROW_CONTEXT_INVALID", item.Issues[0].Code),
        item => Assert.Equal("RESOURCE_VERSION_STALE", item.Issues[0].Code),
        item => Assert.Equal("ACTION_INELIGIBLE", item.Issues[0].Code));

      var json = JsonSerializer.Serialize(envelopes[4], SerializerOptions());
      var issue = JsonNode.Parse(json)!["issues"]![0]!;

      Assert.Equal("error", issue["severity"]!.GetValue<string>());
      Assert.Null(issue["correlationId"]);
      Assert.True(JsonNode.DeepEquals(ReadFixture("operation-context.error.json"), JsonNode.Parse(json)));
    }

    static JsonNode ReadFixture(string name) =>
      JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ScanProcessing", name)));

    static OperationRowContext CreateContext(string origin, RollScanState scanState) =>
      OperationRowContextFactory.Create(
        new KnownClient("Example job", "JOB-001", ClientId, Id.From("00000000-0000-0000-0000-000000000001")),
        new KnownRoll("Roll A", RollId, BoxId),
        new MicrofilmTableRow("row-1", RollId.ToString(), origin, new Dictionary<string, MicrofilmCellValue>()),
        scanState,
        scanState == RollScanState.Idle ? null : "scan-0001",
        "rv1-0000000000000002");

    static JsonSerializerOptions SerializerOptions()
    {
      var options = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };

      options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
      return options;
    }
  }
}
