using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Outermind.Microfilm;
using Outermind.Microfilm.Queries;
using Outermind.Microfilm.Topics;
using Totem.App.Tests;
using Xunit;

namespace Quantum.Tests
{
  public sealed class ScanProcessingAccessTopicTests : TopicTests<ScanProcessingAccessTopic>
  {
    [Fact]
    public async Task DefineRole_NormalizesCatalogAndWritesRedactedAuditFact()
    {
      await Append(new DefineScanProcessingRole(
        "request-1",
        Manager(),
        " Scan Operator ",
        " Starts scans ",
        new[] { "SCAN.START", ScanProcessingPermissions.ScanDiscover, ScanProcessingPermissions.ScanStart }));

      var defined = await Expect<ScanProcessingRoleDefined>();
      var audit = await Expect<ScanProcessingAccessAuditRecorded>();

      Assert.Equal("Scan Operator", defined.Role.Name);
      Assert.Equal("Starts scans", defined.Role.Description);
      Assert.Equal(new[] { ScanProcessingPermissions.ScanDiscover, ScanProcessingPermissions.ScanStart }, defined.Role.Permissions);
      Assert.Equal(1, defined.Role.Version);
      Assert.Equal(1, defined.AuthorizationRevision);
      Assert.Equal(ScanProcessingAccessActions.RoleDefine, audit.Action);
      Assert.Equal("accepted", audit.Outcome);
      Assert.Equal(defined.Role.RoleId, audit.RoleId);
      Assert.Equal(defined.Role.Permissions, audit.ResultingPermissions);
      Assert.DoesNotContain("secret", audit.GetType().GetProperties().Select(property => property.Name), System.StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RoleDefinition_UsesExpectedVersionAndDurableRejections()
    {
      var role = await DefineRole(ScanProcessingPermissions.ScanStart);

      await Append(new ReplaceScanProcessingRole(
        "request-2",
        Manager(),
        role.Role.RoleId,
        1,
        "Scan Lead",
        null,
        new[] { ScanProcessingPermissions.ScanStart, ScanProcessingPermissions.ScanFinishAny }));

      var changed = await Expect<ScanProcessingRoleDefinitionChanged>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      Assert.Equal(2, changed.Role.Version);
      Assert.Equal(2, changed.AuthorizationRevision);

      await Append(new ReplaceScanProcessingRole(
        "request-3",
        Manager(),
        role.Role.RoleId,
        1,
        "Stale edit",
        null,
        new[] { ScanProcessingPermissions.ScanStart }));

      var rejected = await Expect<ScanProcessingAccessRequestRejected>();
      var audit = await Expect<ScanProcessingAccessAuditRecorded>();

      Assert.Equal(ScanProcessingAccessIssueCodes.RoleVersionStale, rejected.Code);
      Assert.Equal(2, rejected.AuthorizationRevision);
      Assert.Equal("rejected", audit.Outcome);
      Assert.Equal(rejected.Code, audit.Code);
    }

    [Fact]
    public async Task Authorization_UsesDurableAssignmentAndStableUserId_NotPresentationIdentity()
    {
      var role = await DefineRole(ScanProcessingPermissions.ScanStart);
      await AssignRole(role.Role.RoleId, Operator());

      var sameStableActorWithChangedLabels = new ScanProcessingActorIdentity(
        Operator().UserId,
        "forged-name",
        "Forged Label",
        "FORGED_PROCESS_ID");

      await Append(new RequestScanProcessingAuthorization(
        "operation-1",
        sameStableActorWithChangedLabels,
        ScanProcessingPermissions.ScanStart,
        Scope("roll-1", "row-1")));

      var authorized = await Expect<ScanProcessingOperationAuthorized>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      Assert.Equal(role.Role.RoleId, Assert.Single(authorized.EffectiveRoleIds));
      Assert.Equal(2, authorized.AuthorizationRevision);

      var forgedStableId = new ScanProcessingActorIdentity(
        "user-attacker",
        Operator().UserName,
        Operator().DisplayName,
        Operator().ProcessUserId);

      await Append(new RequestScanProcessingAuthorization(
        "operation-2",
        forgedStableId,
        ScanProcessingPermissions.ScanStart,
        Scope("roll-1", "row-1")));

      var rejected = await Expect<ScanProcessingOperationAuthorizationRejected>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      Assert.Equal(ScanProcessingAccessIssueCodes.ForbiddenOperation, rejected.Code);
      Assert.Equal("user-attacker", rejected.Actor.UserId);
      Assert.DoesNotContain(
        typeof(RequestScanProcessingAuthorization).GetProperties(),
        property => property.Name is "Roles" or "RoleIds" or "Permissions");
    }

    [Fact]
    public async Task Revocation_IsForwardOnlyAndOrdersLaterDecisionsAfterTheRevocation()
    {
      var role = await DefineRole(ScanProcessingPermissions.ScanStart);
      await AssignRole(role.Role.RoleId, Operator());

      await Append(new RequestScanProcessingAuthorization(
        "operation-before-revoke",
        Operator(),
        ScanProcessingPermissions.ScanStart,
        Scope("roll-1", "row-1")));

      var acceptedBefore = await Expect<ScanProcessingOperationAuthorized>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      await Append(new RevokeScanProcessingRole(
        "revoke-1",
        Manager(),
        Operator(),
        role.Role.RoleId));

      var revoked = await Expect<ScanProcessingRoleRevoked>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      await Append(new RequestScanProcessingAuthorization(
        "operation-after-revoke",
        Operator(),
        ScanProcessingPermissions.ScanStart,
        Scope("roll-1", "row-1")));

      var rejectedAfter = await Expect<ScanProcessingOperationAuthorizationRejected>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      Assert.Equal(2, acceptedBefore.AuthorizationRevision);
      Assert.Equal(3, revoked.AuthorizationRevision);
      Assert.Equal(revoked.AuthorizationRevision, rejectedAfter.AuthorizationRevision);
      Assert.Equal(ScanProcessingAccessIssueCodes.ForbiddenOperation, rejectedAfter.Code);
    }

    [Fact]
    public async Task PermissionRemoval_TakesEffectForDecisionsOrderedAfterRoleVersionChange()
    {
      var role = await DefineRole(ScanProcessingPermissions.ScanStart);
      await AssignRole(role.Role.RoleId, Operator());

      await Append(new ReplaceScanProcessingRole(
        "replace-1",
        Manager(),
        role.Role.RoleId,
        1,
        role.Role.Name,
        null,
        new[] { ScanProcessingPermissions.ScanDiscover }));
      var changed = await Expect<ScanProcessingRoleDefinitionChanged>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      await Append(new RequestScanProcessingAuthorization(
        "operation-1",
        Operator(),
        ScanProcessingPermissions.ScanStart,
        Scope("roll-1", "row-1")));
      var rejected = await Expect<ScanProcessingOperationAuthorizationRejected>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      Assert.Equal(changed.AuthorizationRevision, rejected.AuthorizationRevision);
      Assert.Equal(ScanProcessingAccessIssueCodes.ForbiddenOperation, rejected.Code);
    }

    [Fact]
    public async Task ManagerStatusOrRegistrationAlone_DoesNotGrantOperationPermission()
    {
      await Append(new GrantScanProcessingManager("bootstrap-1", Operator()));
      var granted = await Expect<ScanProcessingManagerGranted>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      await Append(new RequestScanProcessingAuthorization(
        "operation-1",
        Operator(),
        ScanProcessingPermissions.ProcessingDiscover,
        Scope("roll-1", "row-1")));
      var rejected = await Expect<ScanProcessingOperationAuthorizationRejected>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      Assert.Equal(1, granted.AuthorizationRevision);
      Assert.Equal(granted.AuthorizationRevision, rejected.AuthorizationRevision);
      Assert.Equal(ScanProcessingAccessIssueCodes.ForbiddenOperation, rejected.Code);
    }

    [Fact]
    public async Task ConflictingResolvedClientAndWorkspaceScope_IsRejectedEvenWithPermission()
    {
      var role = await DefineRole(ScanProcessingPermissions.ScanStart);
      await AssignRole(role.Role.RoleId, Operator());

      await Append(new RequestScanProcessingAuthorization(
        "operation-1",
        Operator(),
        ScanProcessingPermissions.ScanStart,
        new ScanProcessingResolvedScope("client-1", "client-forged", "roll-1", "row-1")));

      var rejected = await Expect<ScanProcessingOperationAuthorizationRejected>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      Assert.Equal(ScanProcessingAccessIssueCodes.ForbiddenOperation, rejected.Code);
      Assert.Equal(2, rejected.AuthorizationRevision);
    }

    [Fact]
    public async Task UnknownPermission_IsRejectedBeforeItCanEnterARole()
    {
      await Append(new DefineScanProcessingRole(
        "request-1",
        Manager(),
        "Unsafe role",
        null,
        new[] { "filesystem.path.authority" }));

      var rejected = await Expect<ScanProcessingAccessRequestRejected>();
      await Expect<ScanProcessingAccessAuditRecorded>();

      Assert.Equal(ScanProcessingAccessIssueCodes.InvalidPermission, rejected.Code);
      Assert.Equal(0, rejected.AuthorizationRevision);
    }

    async Task<ScanProcessingRoleDefined> DefineRole(params string[] permissions)
    {
      await Append(new DefineScanProcessingRole(
        $"define-{permissions[0]}",
        Manager(),
        "Scan operator",
        null,
        permissions));

      var role = await Expect<ScanProcessingRoleDefined>();
      await Expect<ScanProcessingAccessAuditRecorded>();
      return role;
    }

    async Task AssignRole(string roleId, ScanProcessingActorIdentity target)
    {
      await Append(new AssignScanProcessingRole("assign-1", Manager(), target, roleId));
      await Expect<ScanProcessingRoleAssigned>();
      await Expect<ScanProcessingAccessAuditRecorded>();
    }

    static ScanProcessingActorIdentity Manager() =>
      new("user-manager", "manager", "Demo Manager", "MANAGER");

    static ScanProcessingActorIdentity Operator() =>
      new("user-operator", "operator", "Demo Operator", "OPERATOR");

    static ScanProcessingResolvedScope Scope(string rollId, string rowId) =>
      new("client-1", "client-1", rollId, rowId);
  }

  public sealed class ScanProcessingAccessQueryTests : QueryTests<ScanProcessingAccessQuery>
  {
    [Fact]
    public async Task ReplaysRolesAssignmentsRevocationsAndManagerStatus()
    {
      var roleV1 = new ScanProcessingRoleDefinition(
        "role-1",
        "Scan operator",
        null,
        new[] { ScanProcessingPermissions.ScanStart },
        1);
      var roleV2 = new ScanProcessingRoleDefinition(
        "role-1",
        "Scan operator",
        null,
        new[] { ScanProcessingPermissions.ScanDiscover },
        2);
      var actor = new ScanProcessingActorIdentity("user-1", "operator", "Operator", "OPERATOR");

      await Append(new ScanProcessingRoleDefined("request-1", actor, roleV1, 1));
      await Append(new ScanProcessingRoleAssigned("request-2", actor, actor, roleV1.RoleId, new string[0], new[] { roleV1.RoleId }, 2));
      await Append(new ScanProcessingRoleDefinitionChanged("request-3", actor, roleV2, 3));
      await Append(new ScanProcessingRoleRevoked("request-4", actor, actor, roleV1.RoleId, new[] { roleV1.RoleId }, new string[0], 4));
      await Append(new ScanProcessingManagerGranted("request-5", actor, 5));

      var query = await GetQuery();

      Assert.Equal(5, query.State.AuthorizationRevision);
      Assert.Equal(2, query.State.RolesById[roleV1.RoleId].Version);
      Assert.Empty(query.State.ActorsById[actor.UserId].RoleIds);
      Assert.True(query.State.ActorsById[actor.UserId].IsManager);
      Assert.Empty(query.State.ResolveEffectiveRoles(actor.UserId, ScanProcessingPermissions.ScanStart));
    }

    [Fact]
    public void IndependentReplaysProduceTheSameAuthorizationState()
    {
      var actor = new ScanProcessingActorIdentity("user-1", "operator", "Operator", "OPERATOR");
      var role = new ScanProcessingRoleDefinition(
        "role-1",
        "Scan operator",
        null,
        new[] { ScanProcessingPermissions.ScanStart },
        1);
      var defined = new ScanProcessingRoleDefined("request-1", actor, role, 1);
      var assigned = new ScanProcessingRoleAssigned(
        "request-2",
        actor,
        actor,
        role.RoleId,
        new string[0],
        new[] { role.RoleId },
        2);
      var firstInstance = new ScanProcessingAccessState();
      var secondInstance = new ScanProcessingAccessState();

      firstInstance.Apply(defined);
      firstInstance.Apply(assigned);
      secondInstance.Apply(defined);
      secondInstance.Apply(assigned);

      Assert.Equal(firstInstance.AuthorizationRevision, secondInstance.AuthorizationRevision);
      Assert.Equal(
        firstInstance.ResolveEffectiveRoles(actor.UserId, ScanProcessingPermissions.ScanStart),
        secondInstance.ResolveEffectiveRoles(actor.UserId, ScanProcessingPermissions.ScanStart));
    }
  }

  public sealed class ScanProcessingAuthorizationDecisionQueryTests : QueryTests<ScanProcessingAuthorizationDecisionQuery>
  {
    [Fact]
    public async Task ProjectsDecisionByRequestIdForReconciliation()
    {
      var actor = new ScanProcessingActorIdentity("user-1", "operator", "Operator", "OPERATOR");
      var scope = new ScanProcessingResolvedScope("client-1", "client-1", "roll-1", "row-1");

      await Append(new ScanProcessingOperationAuthorized(
        "operation-1",
        actor,
        ScanProcessingPermissions.ScanStart,
        scope,
        new[] { "role-1" },
        7));

      var query = await GetQuery(Totem.Id.From("operation-1"));

      Assert.True(query.Authorized);
      Assert.Equal("operation-1", query.RequestId);
      Assert.Equal(7, query.AuthorizationRevision);
      Assert.Equal("role-1", Assert.Single(query.EffectiveRoleIds));
      Assert.Equal("roll-1", query.ResolvedScope.RollId);
    }
  }

  public sealed class ScanProcessingAccessContractFixtureTests
  {
    static readonly JsonSerializerOptions JsonOptions = new()
    {
      PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void PermissionFixtureMatchesTheFixedVersionOneCatalog()
    {
      var response = Read<Quantum.Web.Controllers.ScanProcessingPermissionCatalogResponse>("access.permissions.json");

      Assert.Equal(ScanProcessingPermissions.All, response.Permissions);
    }

    [Fact]
    public void AccessFixturesAreTypedDeterministicAndSecretFree()
    {
      var roles = Read<Quantum.Web.Controllers.ScanProcessingRolesResponse>("access.roles.json");
      var assignments = Read<Quantum.Web.Controllers.ScanProcessingAssignmentsResponse>("access.assignments.json");
      var bootstrap = Read<Quantum.Web.Controllers.BootstrapScanProcessingManagerResponse>("access.bootstrap.json");

      Assert.Equal(3, roles.AuthorizationRevision);
      Assert.Equal(2, Assert.Single(roles.Roles).Version);
      Assert.Equal(3, assignments.AuthorizationRevision);
      Assert.Equal(2, assignments.Assignments.Count);
      Assert.True(assignments.Assignments.Single(assignment => assignment.Actor.UserName == "demo-manager").IsManager);
      Assert.True(bootstrap.IsManager);

      foreach(var file in new[] { "access.permissions.json", "access.roles.json", "access.assignments.json", "access.bootstrap.json" })
      {
        Assert.DoesNotContain("secret", File.ReadAllText(FixturePath(file)), System.StringComparison.OrdinalIgnoreCase);
      }
    }

    static T Read<T>(string name) =>
      JsonSerializer.Deserialize<T>(File.ReadAllText(FixturePath(name)), JsonOptions);

    static string FixturePath(string name) =>
      Path.Combine(AppContext.BaseDirectory, "Fixtures", "ScanProcessing", name);
  }
}
