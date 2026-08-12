using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Routing;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Outermind.Microfilm.Topics;
using Quantum.Web.Controllers;
using Totem;
using Totem.App.Tests;
using Xunit;

namespace Quantum.Tests
{
  public sealed class ScanProcessingStorageBindingTopicTests : TopicTests<ScanProcessingStorageBindingTopic>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000801");

    [Fact]
    public async Task CreateAndActivate_OrdersOneActiveBindingAndExplicitGenerations()
    {
      await Append(ClientCreated());
      var first = await Create("binding-a", "Primary storage");

      await Append(new ActivateScanProcessingStorageBinding(
        "activate-a-1",
        Actor(),
        ClientId,
        first.Binding.BindingId,
        1,
        first.StorageRevision));

      var activatedA = await Expect<ScanProcessingStorageBindingActivated>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();

      Assert.Equal(1, activatedA.ConfigurationGeneration);
      Assert.Equal(2, activatedA.StorageRevision);
      Assert.Null(activatedA.PreviousBindingId);

      var second = await Create("binding-b", "Alternate storage");

      await Append(new ActivateScanProcessingStorageBinding(
        "activate-b-1",
        Actor(),
        ClientId,
        second.Binding.BindingId,
        1,
        second.StorageRevision));

      var activatedB = await Expect<ScanProcessingStorageBindingActivated>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();

      Assert.Equal("binding-a", activatedB.PreviousBindingId);
      Assert.Equal(1, activatedB.PreviousConfigurationGeneration);
      Assert.Equal(4, activatedB.StorageRevision);
      Assert.Equal(ClientId.ToString(), activatedB.ClientId);
      Assert.Equal(activatedB.ClientId, activatedB.WorkspaceId);
    }

    [Fact]
    public async Task Activation_RejectsStaleRevisionAndRequiresNextGenerationOnReactivation()
    {
      await Append(ClientCreated());
      var created = await Create("binding-a", "Primary storage");

      await Append(new ActivateScanProcessingStorageBinding(
        "activate-stale-revision",
        Actor(),
        ClientId,
        created.Binding.BindingId,
        1,
        0));

      var staleRevision = await Expect<ScanProcessingStorageBindingRequestRejected>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.StorageRevisionStale, staleRevision.Code);

      await Append(new ActivateScanProcessingStorageBinding(
        "activate-1",
        Actor(),
        ClientId,
        created.Binding.BindingId,
        1,
        created.StorageRevision));
      var activated = await Expect<ScanProcessingStorageBindingActivated>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();

      await Append(new ActivateScanProcessingStorageBinding(
        "activate-skip-generation",
        Actor(),
        ClientId,
        created.Binding.BindingId,
        3,
        activated.StorageRevision));

      var staleGeneration = await Expect<ScanProcessingStorageBindingRequestRejected>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ConfigurationGenerationStale, staleGeneration.Code);

      await Append(new ActivateScanProcessingStorageBinding(
        "activate-2",
        Actor(),
        ClientId,
        created.Binding.BindingId,
        2,
        activated.StorageRevision));

      var generationTwo = await Expect<ScanProcessingStorageBindingActivated>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();
      Assert.Equal(2, generationTwo.ConfigurationGeneration);
    }

    [Fact]
    public async Task RepeatedCurrentActivation_IsDurablyUnchanged()
    {
      await Append(ClientCreated());
      var created = await Create("binding-a", "Primary storage");
      await Append(new ActivateScanProcessingStorageBinding(
        "activate-1",
        Actor(),
        ClientId,
        created.Binding.BindingId,
        1,
        created.StorageRevision));
      var activated = await Expect<ScanProcessingStorageBindingActivated>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();

      await Append(new ActivateScanProcessingStorageBinding(
        "activate-repeat",
        Actor(),
        ClientId,
        created.Binding.BindingId,
        1,
        activated.StorageRevision));

      var unchanged = await Expect<ScanProcessingStorageBindingActivationUnchanged>();
      var audit = await Expect<ScanProcessingStorageBindingAuditRecorded>();
      Assert.Equal(activated.StorageRevision, unchanged.StorageRevision);
      Assert.Equal("unchanged", audit.Outcome);
    }

    [Fact]
    public async Task UnknownClientAndDuplicateBinding_AreDurablyRejected()
    {
      await Append(new CreateScanProcessingStorageBinding(
        "create-unknown",
        Actor(),
        ClientId,
        "binding-a",
        "Primary storage",
        Capabilities()));

      var unknown = await Expect<ScanProcessingStorageBindingRequestRejected>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ClientNotFound, unknown.Code);

      await Append(ClientCreated());
      await Create("binding-a", "Primary storage");
      await Append(new CreateScanProcessingStorageBinding(
        "create-duplicate",
        Actor(),
        ClientId,
        "binding-a",
        "Different label",
        Capabilities()));

      var duplicate = await Expect<ScanProcessingStorageBindingRequestRejected>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.BindingAlreadyExists, duplicate.Code);
    }

    [Fact]
    public async Task PathShapedLabels_AreRejectedWithoutLeakingTheRawValue()
    {
      const string ForgedPhysicalRoot = @"Z:\forged-root\child";

      await Append(ClientCreated());
      await Append(new CreateScanProcessingStorageBinding(
        "create-forged-path",
        Actor(),
        ClientId,
        "binding-a",
        ForgedPhysicalRoot,
        Capabilities()));

      var rejected = await Expect<ScanProcessingStorageBindingRequestRejected>();
      var audit = await Expect<ScanProcessingStorageBindingAuditRecorded>();
      var serialized = JsonSerializer.Serialize(new object[] { rejected, audit });

      Assert.Equal(ScanProcessingStorageBindingIssueCodes.InvalidLabel, rejected.Code);
      Assert.DoesNotContain(ForgedPhysicalRoot, serialized, StringComparison.Ordinal);
      Assert.DoesNotContain(
        typeof(ScanProcessingStorageBindingAuditRecorded).GetProperties(),
        property => ContainsForbiddenStorageAuthorityName(property.Name));
    }

    [Fact]
    public async Task CapabilitySet_RequiresFourDistinctLogicalIdentities()
    {
      await Append(ClientCreated());
      var duplicated = Capabilities();
      duplicated[3] = new ScanProcessingStorageCapability(
        ScanProcessingStorageCapabilities.GrayscaleFrames,
        "Duplicate grayscale");

      await Append(new CreateScanProcessingStorageBinding(
        "create-invalid-capabilities",
        Actor(),
        ClientId,
        "binding-a",
        "Primary storage",
        duplicated));

      var rejected = await Expect<ScanProcessingStorageBindingRequestRejected>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.InvalidCapabilities, rejected.Code);
    }

    async Task<ScanProcessingStorageBindingCreated> Create(string bindingId, string label)
    {
      await Append(new CreateScanProcessingStorageBinding(
        $"create-{bindingId}",
        Actor(),
        ClientId,
        bindingId,
        label,
        Capabilities()));

      var created = await Expect<ScanProcessingStorageBindingCreated>();
      await Expect<ScanProcessingStorageBindingAuditRecorded>();
      return created;
    }

    static ClientCreated ClientCreated() =>
      new(new KnownClient("Fixture client", "FIXTURE-001", ClientId, Id.From("server-is-separate")));

    static ScanProcessingActorIdentity Actor() =>
      new("user-admin", "developer-admin", "Developer Admin", "DEVELOPER_ADMIN");

    internal static List<ScanProcessingStorageCapability> Capabilities() => new()
    {
      new(ScanProcessingStorageCapabilities.ScanParent, "Scan parent"),
      new(ScanProcessingStorageCapabilities.Qpf, "QPF files"),
      new(ScanProcessingStorageCapabilities.GrayscaleFrames, "Grayscale Frames"),
      new(ScanProcessingStorageCapabilities.BitonalFrames, "Bitonal Frames")
    };

    internal static bool ContainsForbiddenStorageAuthorityName(string name) =>
      name.Contains("Path", StringComparison.OrdinalIgnoreCase)
      || name.Contains("Root", StringComparison.OrdinalIgnoreCase)
      || name.Contains("Server", StringComparison.OrdinalIgnoreCase);
  }

  public sealed class ScanProcessingStorageBindingQueryTests : QueryTests<ScanProcessingStorageBindingQuery>
  {
    static readonly Id ClientId = Id.From("00000000-0000-0000-0000-000000000801");

    [Fact]
    public async Task ReplayYieldsOneActiveBindingAndPreservesDistinctCapabilities()
    {
      var client = ClientCreated();
      var bindingA = Binding("binding-a", "Primary storage", 1);
      var bindingB = Binding("binding-b", "Alternate storage", 3);

      await Append(client);
      await Append(new ScanProcessingStorageBindingCreated("create-a", Actor(), ClientId.ToString(), ClientId.ToString(), bindingA, 1));
      await Append(new ScanProcessingStorageBindingActivated("activate-a", Actor(), ClientId.ToString(), ClientId.ToString(), "binding-a", 1, null, null, 2));
      await Append(new ScanProcessingStorageBindingCreated("create-b", Actor(), ClientId.ToString(), ClientId.ToString(), bindingB, 3));
      await Append(new ScanProcessingStorageBindingActivated("activate-b", Actor(), ClientId.ToString(), ClientId.ToString(), "binding-b", 1, "binding-a", 1, 4));

      var query = await GetQuery(ClientId);

      Assert.Equal(4, query.State.StorageRevision);
      Assert.Equal("binding-b", query.State.ActiveBinding.BindingId);
      Assert.Equal(1, query.State.ActiveBinding.ConfigurationGeneration);
      Assert.Equal(2, query.State.BindingsById.Count);
      Assert.Equal(
        ScanProcessingStorageCapabilities.All.OrderBy(value => value, StringComparer.Ordinal),
        query.State.BindingsById["binding-b"].Capabilities.Select(capability => capability.Kind));
      Assert.DoesNotContain(
        typeof(ScanProcessingStorageBindingDefinition).GetProperties(),
        property => ScanProcessingStorageBindingTopicTests.ContainsForbiddenStorageAuthorityName(property.Name));
    }

    [Fact]
    public void IndependentReducersReplayTheSameOrderedState()
    {
      var created = new ScanProcessingStorageBindingCreated(
        "create-a",
        Actor(),
        ClientId.ToString(),
        ClientId.ToString(),
        Binding("binding-a", "Primary storage", 1),
        1);
      var activated = new ScanProcessingStorageBindingActivated(
        "activate-a",
        Actor(),
        ClientId.ToString(),
        ClientId.ToString(),
        "binding-a",
        1,
        null,
        null,
        2);
      var first = new ScanProcessingStorageBindingState();
      var second = new ScanProcessingStorageBindingState();

      first.Apply(ClientCreated());
      first.Apply(created);
      first.Apply(activated);
      second.Apply(ClientCreated());
      second.Apply(created);
      second.Apply(activated);

      Assert.Equal(first.StorageRevision, second.StorageRevision);
      Assert.Equal(first.ActiveBinding.BindingId, second.ActiveBinding.BindingId);
      Assert.Equal(first.ActiveBinding.ConfigurationGeneration, second.ActiveBinding.ConfigurationGeneration);
      Assert.Equal(first.BindingsById.Keys, second.BindingsById.Keys);
    }

    static ScanProcessingStorageBindingDefinition Binding(string id, string label, long revision) =>
      new(
        id,
        ClientId.ToString(),
        ClientId.ToString(),
        label,
        ScanProcessingStorageBindingTopicTests.Capabilities(),
        1,
        revision);

    static ClientCreated ClientCreated() =>
      new(new KnownClient("Fixture client", "FIXTURE-001", ClientId, Id.From("server-is-separate")));

    static ScanProcessingActorIdentity Actor() =>
      new("user-admin", "developer-admin", "Developer Admin", "DEVELOPER_ADMIN");
  }

  public sealed class ScanProcessingResourceReferenceTests
  {
    [Fact]
    public void Reference_IsOpaqueBoundAndHasNoAutomaticVersionOneExpiry()
    {
      var context = Context();

      var issued = ScanProcessingResourceReferences.TryIssue(
        "resource-opaque-1",
        context.Actor,
        context.Scope,
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage,
        out var reference);

      Assert.True(issued);
      Assert.Equal(context.Actor.UserId, reference.ActorId);
      Assert.Equal(context.Scope, reference.Scope);
      Assert.Equal("binding-a", reference.BindingId);
      Assert.Equal(1, reference.ConfigurationGeneration);
      Assert.Equal(context.Access.AuthorizationRevision, reference.AuthorizationRevision);
      Assert.Null(reference.ExpiresAt);
      Assert.True(ScanProcessingResourceReferences.ValidateUse(
        reference,
        context.Actor,
        context.Scope,
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage).IsValid);
    }

    [Fact]
    public void WrongActorScopePurposeAndForgedCapabilityFailUseTimeValidation()
    {
      var context = Context();
      var reference = Issue(context);

      var wrongActor = ScanProcessingResourceReferences.ValidateUse(
        reference,
        new ScanProcessingActorIdentity("user-forged", context.Actor.UserName, "Forged", "FORGED"),
        context.Scope,
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage);
      var wrongScope = ScanProcessingResourceReferences.ValidateUse(
        reference,
        context.Actor,
        new ScanProcessingResolvedScope(context.Scope.ClientId, context.Scope.WorkspaceId, "roll-forged", "row-1"),
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage);
      var wrongWorkspace = ScanProcessingResourceReferences.ValidateUse(
        reference,
        context.Actor,
        new ScanProcessingResolvedScope(context.Scope.ClientId, "client-forged", "roll-1", "row-1"),
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage);
      var wrongPurpose = ScanProcessingResourceReferences.ValidateUse(
        reference,
        context.Actor,
        context.Scope,
        "processing.preview",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage);
      var wrongCapability = ScanProcessingResourceReferences.ValidateUse(
        reference,
        context.Actor,
        context.Scope,
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.Qpf,
        context.Access,
        context.Storage);

      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ResourceReferenceScopeMismatch, wrongActor.Code);
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ResourceReferenceScopeMismatch, wrongScope.Code);
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ResourceReferenceScopeMismatch, wrongWorkspace.Code);
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ResourceReferencePurposeMismatch, wrongPurpose.Code);
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ResourceReferenceInvalid, wrongCapability.Code);
    }

    [Fact]
    public void PermissionRevocationAndAuthorizationRevisionChangesInvalidateUse()
    {
      var context = Context();
      var reference = Issue(context);
      var role = context.Access.RolesById["role-scan"];

      context.Access.Apply(new ScanProcessingRoleRevoked(
        "revoke",
        context.Actor,
        context.Actor,
        role.RoleId,
        new[] { role.RoleId },
        Array.Empty<string>(),
        3));

      var result = ScanProcessingResourceReferences.ValidateUse(
        reference,
        context.Actor,
        context.Scope,
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage);

      Assert.False(result.IsValid);
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ResourceReferencePermissionStale, result.Code);
    }

    [Fact]
    public void RolePermissionRemovalInvalidatesUse()
    {
      var context = Context();
      var reference = Issue(context);
      var role = context.Access.RolesById["role-scan"];

      context.Access.Apply(new ScanProcessingRoleDefinitionChanged(
        "replace-role",
        context.Actor,
        new ScanProcessingRoleDefinition(role.RoleId, role.Name, role.Description, Array.Empty<string>(), 2),
        3));

      var result = ScanProcessingResourceReferences.ValidateUse(
        reference,
        context.Actor,
        context.Scope,
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage);

      Assert.False(result.IsValid);
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ResourceReferencePermissionStale, result.Code);
    }

    [Fact]
    public void BindingGenerationChangeAndExplicitReferenceRevocationInvalidateUse()
    {
      var context = Context();
      var reference = Issue(context);

      context.Storage.Apply(new ScanProcessingStorageBindingActivated(
        "activate-generation-2",
        context.Actor,
        context.Scope.ClientId,
        context.Scope.WorkspaceId,
        "binding-a",
        2,
        "binding-a",
        1,
        3));

      var generationResult = ScanProcessingResourceReferences.ValidateUse(
        reference,
        context.Actor,
        context.Scope,
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage);
      reference.Revoked = true;
      var revokedResult = ScanProcessingResourceReferences.ValidateUse(
        reference,
        context.Actor,
        context.Scope,
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage);

      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ResourceReferenceGenerationStale, generationResult.Code);
      Assert.Equal(ScanProcessingStorageBindingIssueCodes.ResourceReferenceRevoked, revokedResult.Code);
    }

    static ScanProcessingResourceReference Issue(ResourceContext context)
    {
      Assert.True(ScanProcessingResourceReferences.TryIssue(
        "resource-opaque-1",
        context.Actor,
        context.Scope,
        "scan.discovery.parent",
        ScanProcessingPermissions.ScanDiscover,
        ScanProcessingStorageCapabilities.ScanParent,
        context.Access,
        context.Storage,
        out var reference));
      return reference;
    }

    static ResourceContext Context()
    {
      var actor = new ScanProcessingActorIdentity("user-operator", "operator", "Operator", "OPERATOR");
      var scope = new ScanProcessingResolvedScope("client-1", "client-1", "roll-1", "row-1");
      var access = new ScanProcessingAccessState();
      var role = new ScanProcessingRoleDefinition(
        "role-scan",
        "Scan operator",
        null,
        new[] { ScanProcessingPermissions.ScanDiscover },
        1);
      access.Apply(new ScanProcessingRoleDefined("define", actor, role, 1));
      access.Apply(new ScanProcessingRoleAssigned(
        "assign",
        actor,
        actor,
        role.RoleId,
        Array.Empty<string>(),
        new[] { role.RoleId },
        2));

      var storage = new ScanProcessingStorageBindingState
      {
        ClientId = scope.ClientId,
        WorkspaceId = scope.WorkspaceId,
        ClientRecognized = true
      };
      var binding = new ScanProcessingStorageBindingDefinition(
        "binding-a",
        scope.ClientId,
        scope.WorkspaceId,
        "Primary storage",
        ScanProcessingStorageBindingTopicTests.Capabilities(),
        1,
        1);
      storage.Apply(new ScanProcessingStorageBindingCreated(
        "create",
        actor,
        scope.ClientId,
        scope.WorkspaceId,
        binding,
        1));
      storage.Apply(new ScanProcessingStorageBindingActivated(
        "activate",
        actor,
        scope.ClientId,
        scope.WorkspaceId,
        binding.BindingId,
        1,
        null,
        null,
        2));

      return new ResourceContext(actor, scope, access, storage);
    }

    sealed record ResourceContext(
      ScanProcessingActorIdentity Actor,
      ScanProcessingResolvedScope Scope,
      ScanProcessingAccessState Access,
      ScanProcessingStorageBindingState Storage);
  }

  public sealed class ScanProcessingStorageBindingContractTests
  {
    static readonly JsonSerializerOptions JsonOptions = new()
    {
      PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void RoutesExposeOnlyLogicalConfigurationManagement()
    {
      var routes = typeof(ScanProcessingStorageBindingsController)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()
          .Select(attribute => $"{attribute.HttpMethods.Single()} {attribute.Template}"))
        .ToHashSet(StringComparer.Ordinal);

      Assert.Contains("GET ", routes);
      Assert.Contains("POST bindings", routes);
      Assert.Contains("POST bindings/{bindingId}/activate", routes);
      Assert.DoesNotContain(routes, route =>
        route.Contains("browse", StringComparison.OrdinalIgnoreCase)
        || route.Contains("start", StringComparison.OrdinalIgnoreCase)
        || route.Contains("apply", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RequestContractsCannotSupplyAuthorityOrPhysicalConfiguration()
    {
      var requestTypes = new[]
      {
        typeof(CreateScanProcessingStorageBindingRequest),
        typeof(ActivateScanProcessingStorageBindingRequest)
      };

      Assert.DoesNotContain(requestTypes.SelectMany(type => type.GetProperties()), property =>
        property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
        || property.Name.Contains("Root", StringComparison.OrdinalIgnoreCase)
        || property.Name.Contains("Server", StringComparison.OrdinalIgnoreCase)
        || property.Name.Contains("Workspace", StringComparison.OrdinalIgnoreCase)
        || property.Name.Contains("Actor", StringComparison.OrdinalIgnoreCase)
        || property.Name.Contains("Role", StringComparison.OrdinalIgnoreCase)
        || property.Name.Contains("Permission", StringComparison.OrdinalIgnoreCase));

      var forgedJson = "{\"label\":\"Logical label\",\"physicalRoot\":\"Z:\\\\forged\",\"serverId\":\"server-forged\",\"capabilities\":[]}";
      var parsed = JsonSerializer.Deserialize<CreateScanProcessingStorageBindingRequest>(forgedJson, JsonOptions);

      Assert.Equal("Logical label", parsed.Label);
      Assert.Empty(parsed.Capabilities);
    }

    [Fact]
    public void InformationalFullPathExistsOnlyOnOutputShape()
    {
      Assert.Contains(
        typeof(ScanProcessingInformationalPathResponse).GetProperties(),
        property => property.Name == "FullPath");
      Assert.DoesNotContain(
        typeof(ScanProcessingResourceReference).GetProperties(),
        property => property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
          || property.Name.Contains("Root", StringComparison.OrdinalIgnoreCase)
          || property.Name.Contains("Server", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BindingAndReferenceFixturesAreTypedAndSeparatedFromServerAuthority()
    {
      var bindings = Read<ScanProcessingStorageBindingsResponse>("storage.bindings.json");
      var reference = Read<ScanProcessingResourceReferenceResponse>("storage.resource-reference.json");

      Assert.Equal(bindings.ClientId, bindings.WorkspaceId);
      Assert.Equal("binding-primary", bindings.ActiveBinding.BindingId);
      Assert.Equal(4, Assert.Single(bindings.Bindings).Capabilities.Count);
      Assert.Null(reference.ExpiresAt);
      Assert.NotNull(reference.InformationalPath);
      Assert.DoesNotContain("server", File.ReadAllText(FixturePath("storage.bindings.json")), StringComparison.OrdinalIgnoreCase);
    }

    static T Read<T>(string name) =>
      JsonSerializer.Deserialize<T>(File.ReadAllText(FixturePath(name)), JsonOptions);

    static string FixturePath(string name) =>
      Path.Combine(AppContext.BaseDirectory, "Fixtures", "ScanProcessing", name);
  }
}
