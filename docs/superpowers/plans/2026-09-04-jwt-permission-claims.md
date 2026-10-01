# JWT Permission Claims Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Emitir en cada access token los permisos efectivos persistidos y autorizar localmente mediante claims, sin consultar Oracle por cada endpoint protegido.

**Architecture:** Oracle continúa como fuente de verdad. Login, refresh y emisión de identidad delegada calculan la matriz efectiva una vez y la convierten en claims `permissions` con formato `perm:{Módulo}:{Acción}`; el handler HTTP compara la claim requerida localmente. SuperAdmin conserva su bypass por `role_id` y los access tokens expiran en 15 minutos para limitar la ventana de permisos obsoletos.

**Tech Stack:** .NET, ASP.NET Core Authorization, MediatR, EF Core con Oracle, JWT RS256, xUnit y NSubstitute.

## Global Constraints

- Mantener la fórmula actual `permiso del rol OR permiso individual`.
- Persistir cambios de permisos inmediatamente en Oracle.
- Reflejar cambios en el próximo login o refresh; ventana máxima aceptada: 15 minutos.
- Mantener el identificador canónico y bypass del rol SuperAdmin.
- Incluir únicamente permisos concedidos, sin duplicados y en orden determinista.
- Mantener `GET /api/auth/permissions` compatible con el frontend.
- No introducir Redis ni consultas de permisos durante la autorización de endpoints.
- Ejecutar únicamente pruebas dirigidas de seguridad y autenticación, no la suite completa.

---

## File Map

- Create `src/Application/Permissions/Claims/PermissionClaimValue.cs`: contrato común para tipo, formato y lectura de claims.
- Create `src/Application/Permissions/UseCases/GetUserPermissionClaimsQuery.cs`: solicitud del conjunto de claims efectivas.
- Create `src/Application/Permissions/UseCases/GetUserPermissionClaimsQueryHandler.cs`: transforma la matriz efectiva en claims concedidas.
- Modify `src/Application/Permissions/UseCases/GetUserEffectivePermissionsQueryHandler.cs`: calcula toda la matriz con lecturas masivas, sin una consulta por módulo.
- Modify `src/Infrastructure/Security/Tokens/JwtTokenIssuer.cs`: agrega las claims al token RS256.
- Modify `src/Infrastructure/Security/Authentication/AuthenticationService.cs`: resuelve permisos antes de login y refresh.
- Modify `src/Infrastructure/Telegram/Security/AgentDelegatedIdentityProvider.cs`: emite permisos para identidades vinculadas y ninguno para invitados.
- Modify `src/Api/Common/Security/Permissions/RequirePermissionAttribute.cs`: reutiliza el prefijo común.
- Modify `src/Api/Common/Security/Permissions/PermissionAuthorizationHandler.cs`: deja de consultar Oracle y valida claims.
- Modify `src/Api/Auth/Controllers/AuthControllers.cs`: reconstruye la respuesta de permisos desde el JWT vigente.
- Modify `.env.example` and `README.md`: establece y documenta 15 minutos.
- Update focused tests under `tests/Application.Tests/Permissions` and `tests/Api.Tests/Security`.

---

### Task 1: Effective permission matrix without N+1 reads

**Files:**
- Create: `tests/Application.Tests/Permissions/GetUserEffectivePermissionsQueryHandlerTests.cs`
- Modify: `src/Application/Permissions/UseCases/GetUserEffectivePermissionsQueryHandler.cs`

**Interfaces:**
- Consumes: `IUnitOfWork.ModulesRepository.GetAllAsync`, `RolePermissionsRepository.GetByRoleIdAsync`, `UserPermissionsRepository.GetByUserIdAsync`.
- Produces: `Task<IReadOnlyDictionary<string, EffectivePermission>> Handle(GetUserEffectivePermissionsQuery, CancellationToken)` with additive role/user semantics.

- [ ] **Step 1: Write the failing bulk-resolution tests**

Create tests that configure two modules, role permissions for one module and a user grant for another. Assert the four effective flags and verify each bulk repository method is called once. Include a case where role `true` plus user `false` remains `true`.

```csharp
[Fact]
public async Task Handle_combines_role_and_user_rows_with_three_bulk_reads()
{
    modulesRepository.GetAllAsync(Arg.Any<CancellationToken>())
        .Returns([clientsModule, petsModule]);
    rolePermissionsRepository.GetByRoleIdAsync(RoleId, Arg.Any<CancellationToken>())
        .Returns([new RolePermission(RoleId, petsModule.Id, true, false, false, false)]);
    userPermissionsRepository.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
        .Returns([new UserPermission(UserId, clientsModule.Id, true, true, false, false)]);

    var result = await sut.Handle(
        new GetUserEffectivePermissionsQuery(RoleId, UserId),
        CancellationToken.None);

    Assert.True(result["Mascotas"].CanView);
    Assert.True(result["Clientes"].CanCreate);
    await modulesRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    await rolePermissionsRepository.Received(1)
        .GetByRoleIdAsync(RoleId, Arg.Any<CancellationToken>());
    await userPermissionsRepository.Received(1)
        .GetByUserIdAsync(UserId, Arg.Any<CancellationToken>());
}
```

- [ ] **Step 2: Run the new test and confirm the current handler fails the bulk-call assertions**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~GetUserEffectivePermissionsQueryHandlerTests
```

Expected: FAIL because the current handler sends one nested query per module instead of using the three bulk reads.

- [ ] **Step 3: Replace the per-module MediatR loop with an in-memory join**

Load modules, role rows and user rows once. Index rows by `ModuleId`, then calculate each action with `roleFlag || userFlag`. Return an entry for every module, using `EffectivePermission.None` when neither source has a row.

```csharp
var modules = await unitOfWork.ModulesRepository.GetAllAsync(cancellationToken);
var roleRows = await unitOfWork.RolePermissionsRepository
    .GetByRoleIdAsync(request.RoleId, cancellationToken);
var userRows = await unitOfWork.UserPermissionsRepository
    .GetByUserIdAsync(request.UserId, cancellationToken);

var roleByModule = roleRows.ToDictionary(x => x.ModuleId);
var userByModule = userRows.ToDictionary(x => x.ModuleId);

return modules.ToDictionary(
    module => module.Name.Value,
    module =>
    {
        roleByModule.TryGetValue(module.Id, out var role);
        userByModule.TryGetValue(module.Id, out var user);
        return new EffectivePermission(
            (role?.CanView ?? false) || (user?.CanView ?? false),
            (role?.CanCreate ?? false) || (user?.CanCreate ?? false),
            (role?.CanEdit ?? false) || (user?.CanEdit ?? false),
            (role?.CanDelete ?? false) || (user?.CanDelete ?? false));
    });
```

- [ ] **Step 4: Run the two focused permission-handler test classes**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~GetUserEffectivePermissionsQueryHandlerTests|FullyQualifiedName~GetEffectivePermissionQueryHandlerTests"
```

Expected: PASS.

- [ ] **Step 5: Commit the bulk resolver**

```powershell
git add src/Application/Permissions/UseCases/GetUserEffectivePermissionsQueryHandler.cs tests/Application.Tests/Permissions
git commit -m "perf(security): ⚡ resolve permission matrix in bulk"
```

---

### Task 2: Permission claim contract and query

**Files:**
- Create: `src/Application/Permissions/Claims/PermissionClaimValue.cs`
- Create: `src/Application/Permissions/UseCases/GetUserPermissionClaimsQuery.cs`
- Create: `src/Application/Permissions/UseCases/GetUserPermissionClaimsQueryHandler.cs`
- Create: `tests/Application.Tests/Permissions/GetUserPermissionClaimsQueryHandlerTests.cs`

**Interfaces:**
- Produces: `PermissionClaimValue.ClaimType`, `PermissionClaimValue.PolicyPrefix`, `Create(string moduleName, string action)`, and `TryParse(string value, out string moduleName, out string action)`.
- Produces: `GetUserPermissionClaimsQuery(Guid RoleId, Guid UserId)` returning `IReadOnlyCollection<string>`.

- [ ] **Step 1: Write failing tests for formatting and flattening**

Cover exact output, rejected malformed strings, omission of false flags, duplicate removal and ordinal sorting.

```csharp
[Fact]
public async Task Handle_emits_only_granted_actions_in_deterministic_order()
{
    sender.Send(
            new GetUserEffectivePermissionsQuery(RoleId, UserId),
            Arg.Any<CancellationToken>())
        .Returns(new Dictionary<string, EffectivePermission>
        {
            ["Mascotas"] = new(true, true, false, false),
            ["Citas"] = new(true, false, true, false)
        });

    var result = await sut.Handle(
        new GetUserPermissionClaimsQuery(RoleId, UserId),
        CancellationToken.None);

    Assert.Equal(
        ["perm:Citas:Edit", "perm:Citas:View", "perm:Mascotas:Create", "perm:Mascotas:View"],
        result);
}
```

- [ ] **Step 2: Run the claim-query tests and confirm they fail because the types do not exist**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~GetUserPermissionClaimsQueryHandlerTests
```

Expected: FAIL at compile time for the missing claim types.

- [ ] **Step 3: Implement the shared claim formatter**

```csharp
public static class PermissionClaimValue
{
    public const string ClaimType = "permissions";
    public const string PolicyPrefix = "perm:";

    public static string Create(string moduleName, string action) =>
        $"{PolicyPrefix}{moduleName}:{action}";

    public static bool TryParse(string value, out string moduleName, out string action)
    {
        moduleName = string.Empty;
        action = string.Empty;
        if (!value.StartsWith(PolicyPrefix, StringComparison.Ordinal)) return false;
        var remainder = value[PolicyPrefix.Length..];
        var separator = remainder.LastIndexOf(':');
        if (separator <= 0 || separator == remainder.Length - 1) return false;
        moduleName = remainder[..separator];
        action = remainder[(separator + 1)..];
        return true;
    }
}
```

- [ ] **Step 4: Implement the MediatR query and flatten the effective matrix**

The handler sends `GetUserEffectivePermissionsQuery`, creates claims for true flags using action names `View`, `Create`, `Edit`, `Delete`, then returns `Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()`.

- [ ] **Step 5: Run the focused Application tests**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~GetUserPermissionClaimsQueryHandlerTests|FullyQualifiedName~GetUserEffectivePermissionsQueryHandlerTests"
```

Expected: PASS.

- [ ] **Step 6: Commit the claim contract**

```powershell
git add src/Application/Permissions tests/Application.Tests/Permissions
git commit -m "feat(security): ✨ build effective permission claims"
```

---

### Task 3: Emit claims on login, refresh and delegated identities

**Files:**
- Modify: `src/Infrastructure/Security/Tokens/JwtTokenIssuer.cs`
- Modify: `src/Infrastructure/Security/Authentication/AuthenticationService.cs`
- Modify: `src/Infrastructure/Telegram/Security/AgentDelegatedIdentityProvider.cs`
- Modify: `tests/Api.Tests/Security/JwtTokenIssuerTests.cs`
- Modify: `tests/Api.Tests/Security/AuthenticationServiceSuperAdminTests.cs`
- Modify: `tests/Api.Tests/Security/AuthenticationServicePlatformAccessTests.cs`
- Modify: `tests/Api.Tests/Security/SecurityStage1Tests.cs`
- Modify: `tests/Infrastructure.Tests/Telegram/AgentGuestIdentityProviderTests.cs`
- Add or update: `tests/Infrastructure.Tests/Telegram/AgentDelegatedIdentityProviderTests.cs`

**Interfaces:**
- Consumes: `ISender.Send(new GetUserPermissionClaimsQuery(roleId, personId), cancellationToken)`.
- Produces: `JwtTokenIssuer.Issue(AuthenticatedIdentity identity, IReadOnlyCollection<string> permissions)` and `Issue(AuthenticatedIdentity identity, TimeSpan lifetime, IReadOnlyCollection<string> permissions)`.

- [ ] **Step 1: Extend issuer tests with an array claim and duplicate filtering**

```csharp
var issued = issuer.Issue(
    CreateIdentity(),
    ["perm:Mascotas:View", "perm:Mascotas:View", "perm:Citas:Create"]);
var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);

Assert.Equal(
    ["perm:Citas:Create", "perm:Mascotas:View"],
    token.Claims.Where(x => x.Type == PermissionClaimValue.ClaimType)
        .Select(x => x.Value).Order().ToArray());
```

Also assert that a SuperAdmin token does not enumerate permissions.

- [ ] **Step 2: Run issuer tests and confirm failure**

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~JwtTokenIssuerTests
```

Expected: FAIL because the issuer overloads do not yet accept permissions.

- [ ] **Step 3: Add permission claims to `JwtTokenIssuer`**

Normalize with `Where(!string.IsNullOrWhiteSpace)`, `Distinct(StringComparer.Ordinal)` and `Order(StringComparer.Ordinal)`. Add each value as a separate claim with type `PermissionClaimValue.ClaimType`; `JwtSecurityTokenHandler` serializes repeated claim types as the JWT array requested.

- [ ] **Step 4: Resolve claims before issuing authentication tokens**

Inject `ISender` into `AuthenticationService`. In `IssueTokensAsync`, resolve claims using the current `identity.RoleId` and `identity.PersonId`, except for SuperAdmin, then call the new issuer overload. Because both login and refresh already pass through `IssueTokensAsync`, both paths receive current database permissions.

```csharp
var permissions = SystemRoles.IsSuperAdmin(identity.RoleId)
    ? Array.Empty<string>()
    : await sender.Send(
        new GetUserPermissionClaimsQuery(identity.RoleId, identity.PersonId),
        cancellationToken);
var accessToken = jwtTokenIssuer.Issue(identity, permissions);
```

- [ ] **Step 5: Resolve claims for Telegram delegated identities**

Inject `ISender` into `AgentDelegatedIdentityProvider`. Linked identities receive current permissions before token issuance. Guest identities receive an empty collection and retain their short delegated lifetime.

- [ ] **Step 6: Update constructor fixtures and verify login, refresh and Telegram paths**

Configure `ISender` substitutes in every direct constructor test. Add an assertion that login and refresh call `GetUserPermissionClaimsQuery`; assert the linked Telegram path includes permissions and the guest path does not.

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~JwtTokenIssuerTests|FullyQualifiedName~AuthenticationService"
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~AgentGuestIdentityProviderTests|FullyQualifiedName~AgentDelegatedIdentityProviderTests"
```

Expected: PASS.

- [ ] **Step 7: Commit token emission**

```powershell
git add src/Infrastructure/Security src/Infrastructure/Telegram tests/Api.Tests/Security tests/Infrastructure.Tests/Telegram
git commit -m "feat(security): ✨ embed permissions in access tokens"
```

---

### Task 4: Authorize locally from JWT claims

**Files:**
- Modify: `src/Api/Common/Security/Permissions/RequirePermissionAttribute.cs`
- Modify: `src/Api/Common/Security/Permissions/PermissionAuthorizationHandler.cs`
- Modify: `tests/Api.Tests/Security/PermissionAuthorizationHandlerTests.cs`
- Modify: `tests/Api.Tests/Security/RolesAndDiagnosticsAuthorizationTests.cs`

**Interfaces:**
- Consumes: claim type `permissions` and exact value `PermissionClaimValue.Create(requirement.ModuleName, requirement.Action.ToString())`.
- Produces: claim-only authorization with no `ISender` or Oracle access.

- [ ] **Step 1: Rewrite handler tests for claim-based authorization**

Test SuperAdmin bypass, exact granted claim, missing claim, wrong module, wrong action and case mismatch. Remove all `ISender` setup and assert the handler has a parameterless constructor.

```csharp
[Fact]
public async Task HandleAsync_succeeds_for_the_exact_permission_claim()
{
    var requirement = new PermissionRequirement("Citas", PermissionAction.Edit);
    var context = CreateContext(requirement,
    [
        new Claim(PermissionClaimValue.ClaimType, "perm:Citas:Edit")
    ]);

    await new PermissionAuthorizationHandler().HandleAsync(context);

    Assert.True(context.HasSucceeded);
}
```

- [ ] **Step 2: Run handler tests and confirm they fail against the database-backed constructor**

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~PermissionAuthorizationHandlerTests
```

Expected: FAIL because the current handler requires `ISender` and reads effective permissions.

- [ ] **Step 3: Implement exact local claim comparison**

Keep the SuperAdmin branch. For all other users, construct the required claim value and call:

```csharp
if (context.User.HasClaim(
        PermissionClaimValue.ClaimType,
        PermissionClaimValue.Create(
            requirement.ModuleName,
            requirement.Action.ToString())))
{
    context.Succeed(requirement);
}
```

Remove `ISender`, `GetEffectivePermissionQuery`, `role_id` and `person_id` parsing from this handler. Update `RequirePermissionAttribute.PolicyPrefix` to reference the shared constant.

- [ ] **Step 4: Run focused authorization tests**

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~PermissionAuthorizationHandlerTests|FullyQualifiedName~RolesAndDiagnosticsAuthorizationTests|FullyQualifiedName~AuthorizationPoliciesTests"
```

Expected: PASS.

- [ ] **Step 5: Commit local authorization**

```powershell
git add src/Api/Common/Security/Permissions tests/Api.Tests/Security
git commit -m "perf(security): ⚡ authorize permissions from JWT claims"
```

---

### Task 5: Serve the current token permission matrix

**Files:**
- Modify: `src/Api/Auth/Controllers/AuthControllers.cs`
- Modify: `tests/Api.Tests/Security/AuthControllerPermissionsTests.cs`

**Interfaces:**
- Consumes: all claims with type `PermissionClaimValue.ClaimType` from `User`.
- Produces: existing `UserPermissionsResponseDto` contract.

- [ ] **Step 1: Replace the regular-user controller test expectation**

Give the controller claims such as `perm:Clientes:View` and `perm:Clientes:Edit`. Mock `GetAllModulesQuery` to return `Clientes` and `Mascotas`. Assert the response contains both modules, the claimed actions are true, all remaining actions are false, and no effective-permission query is sent.

- [ ] **Step 2: Run the controller tests and confirm the regular-user case fails**

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~AuthControllerPermissionsTests
```

Expected: FAIL because the controller still asks Oracle for the effective matrix.

- [ ] **Step 3: Reconstruct the DTO from claims**

Load the module catalog once to preserve the complete response shape. Initialize every module with false flags, parse valid permission claims, ignore unknown modules/actions and set the corresponding flag. Preserve the current SuperAdmin all-true branch.

- [ ] **Step 4: Verify the endpoint contract tests**

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~AuthControllerPermissionsTests
```

Expected: PASS.

- [ ] **Step 5: Commit endpoint compatibility**

```powershell
git add src/Api/Auth/Controllers/AuthControllers.cs tests/Api.Tests/Security/AuthControllerPermissionsTests.cs
git commit -m "refactor(auth): ♻️ expose permissions from current JWT"
```

---

### Task 6: Set the 15-minute default and run focused verification

**Files:**
- Modify: `.env.example`
- Modify: `README.md`
- Modify if currently different: `src/Infrastructure/Security/Options/JwtOptions.cs`
- Modify: `tests/Api.Tests/Security/JwtOptionsValidatorTests.cs`

**Interfaces:**
- Produces: documented `Jwt__AccessTokenMinutes=15` deployment default.

- [ ] **Step 1: Add or update the configuration assertion**

Assert the example configuration documents `Jwt__AccessTokenMinutes=15`. Keep validator tests proving zero and negative values are rejected.

- [ ] **Step 2: Update environment and security documentation**

Change `.env.example` from `60` to `15`. Explain that permission and role changes reach existing sessions on login, refresh or access-token expiry, with a maximum expected delay of 15 minutes.

- [ ] **Step 3: Run formatting and the reduced security suite**

```powershell
dotnet format veterinarian_backend.slnx --verify-no-changes
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~Permissions
dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~JwtTokenIssuerTests|FullyQualifiedName~PermissionAuthorizationHandlerTests|FullyQualifiedName~AuthControllerPermissionsTests|FullyQualifiedName~AuthenticationService|FullyQualifiedName~AuthorizationPoliciesTests"
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~AgentGuestIdentityProviderTests|FullyQualifiedName~AgentDelegatedIdentityProviderTests"
```

Expected: formatting check and all selected tests PASS.

- [ ] **Step 4: Inspect the final diff for accidental secret or contract changes**

```powershell
git diff --check
git status --short
git diff --stat develop...HEAD
rg -n "PrivateKeyPemBase64|BotToken|WebhookSecret" . --glob '!README.md' --glob '!.env.example' --glob '!bin/**' --glob '!obj/**'
```

Expected: no whitespace errors, no private `.env` or secret values, and only the scoped JWT permission files changed.

- [ ] **Step 5: Commit configuration and documentation**

```powershell
git add .env.example README.md tests/Api.Tests/Security/JwtOptionsValidatorTests.cs src/Infrastructure/Security/Options/JwtOptions.cs
git commit -m "docs(security): 📝 document short-lived permission tokens"
```

---

## Acceptance Checklist

- [ ] Login emits only effective granted permissions.
- [ ] Refresh reloads permissions from Oracle before issuing the next JWT.
- [ ] Linked Telegram identities carry current permissions; guests carry none.
- [ ] Permission-protected endpoints perform no permission query against Oracle.
- [ ] SuperAdmin retains access without enumerated permission claims.
- [ ] `GET /api/auth/permissions` matches the current token.
- [ ] `Jwt__AccessTokenMinutes` is documented as 15.
- [ ] Existing role policies, RS256 validation and refresh-token rotation remain unchanged.
