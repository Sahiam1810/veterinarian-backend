# Persisted SuperAdmin and Catalog Seeds Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the environment-backed synthetic SuperAdmin with a protected persisted role/account flow and provide idempotent Oracle seeds for required system catalogs.

**Architecture:** A canonical role identifier in Domain distinguishes the non-delegable SuperAdmin role without relying on its display name. Authentication uses the existing user/account/credential/token repositories, while API authorization resolves elevated access from the normal JWT `role_id`; Oracle seeds and a separate provisioning script establish catalog data without embedding personal credentials.

**Tech Stack:** .NET 10, ASP.NET Core JWT Bearer, MediatR, FluentValidation, EF Core Oracle provider, xUnit, NSubstitute, Oracle SQL/SQL*Plus.

## Global Constraints

- Work on `fix/persisted-superadmin-catalog-seeds`, created from current `develop`, without a worktree.
- Preserve the existing four-layer dependency direction and public login/refresh/profile response contracts.
- Do not create or apply an EF Core migration; the existing schema already supports the persisted role/account design.
- Do not apply seeds or provisioning scripts to Oracle without an explicitly confirmed target database.
- Never commit a personal email, password, password hash, JWT, Oracle credential, or provider secret.
- General user/role endpoints must never assign, demote, deactivate, rename, or delete the canonical SuperAdmin identity.
- Use focused tests during implementation; do not run the entire test inventory unless the final risk review requires it.
- Use Conventional Commits and the repository's emoji convention.

---

## Baseline

- Backend state before branch: clean `develop` at `0232db3`.
- Build: `dotnet build veterinarian_backend.slnx --no-restore` passed with 0 warnings and 0 errors.
- Focused security baseline: 49 tests passed with 0 failures.
- Chatbot repository has two LF/CRLF metadata-only modifications; they are outside this plan and must remain untouched.
- Compatibility classification: behavior-changing authentication and token semantics; HTTP shapes remain compatible; old synthetic SuperAdmin tokens are intentionally invalidated by the new authorization rule.

## File Structure

- `src/Domain/Roles/SystemRoles.cs`: canonical SuperAdmin role ID/name and role classification helpers.
- `src/Application/*`: guards for role/user/account lifecycle operations.
- `src/Infrastructure/Security/Authentication/AuthenticationService.cs`: one persisted authentication flow for every internal account.
- `src/Infrastructure/Security/Tokens/JwtTokenIssuer.cs`: one JWT issue path with normal identity claims.
- `src/Api/Common/Security/ClaimsPrincipalExtensions.cs`: centralized `role_id`-based SuperAdmin recognition.
- `src/Api/Extensions/AuthorizationExtensions.cs`: policies based on the persisted role identity.
- `database/seeds/*.sql`: idempotent catalog population only.
- `database/admin/promote_superadmin.sql`: explicit account promotion without embedded credentials.
- `database/seeds/apply_all.sql` and `database/seeds/verify_seeds.sql`: ordered execution and verification.
- `tests/Application.Tests` and `tests/Api.Tests`: focused regression coverage in the existing test projects.

---

### Task 1: Canonical system role identity

**Files:**
- Create: `src/Domain/Roles/SystemRoles.cs`
- Create: `tests/Application.Tests/Roles/SystemRolesTests.cs`

**Interfaces:**
- Produces: `SystemRoles.SuperAdminId`, `SystemRoles.SuperAdminName`, `SystemRoles.IsSuperAdmin(Guid)` and `SystemRoles.IsReservedName(string)`.
- Consumes: no infrastructure or API dependency.

- [ ] **Step 1: Write focused failing tests for the canonical identity**

```csharp
[Fact]
public void IsSuperAdmin_accepts_only_the_canonical_identifier()
{
    Assert.True(SystemRoles.IsSuperAdmin(SystemRoles.SuperAdminId));
    Assert.False(SystemRoles.IsSuperAdmin(Guid.NewGuid()));
}

[Theory]
[InlineData("SuperAdmin")]
[InlineData(" superadmin ")]
public void Reserved_name_is_case_insensitive(string name) =>
    Assert.True(SystemRoles.IsReservedName(name));
```

- [ ] **Step 2: Run the focused test and verify that the type is missing**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~SystemRolesTests`

Expected: compilation failure because `SystemRoles` does not exist.

- [ ] **Step 3: Add the canonical role definition**

```csharp
public static class SystemRoles
{
    public static readonly Guid SuperAdminId =
        Guid.Parse("99999999-9999-9999-9999-999999999999");
    public const string SuperAdminName = "SuperAdmin";

    public static bool IsSuperAdmin(Guid roleId) => roleId == SuperAdminId;
    public static bool IsReservedName(string name) =>
        string.Equals(name.Trim(), SuperAdminName, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 4: Run the focused tests and Domain build**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~SystemRolesTests`

Run: `dotnet build src/Domain/Domain.csproj --no-restore`

Expected: all selected tests pass and Domain builds without warnings.

- [ ] **Step 5: Commit the canonical identity**

```bash
git add src/Domain/Roles/SystemRoles.cs tests/Application.Tests/Roles/SystemRolesTests.cs
git commit -m "feat(security): ✨ define persisted SuperAdmin role identity"
```

---

### Task 2: Protect SuperAdmin lifecycle in Application

**Files:**
- Modify: `src/Application/Roles/UseCases/CreateRoleCommandHandler.cs`
- Modify: `src/Application/Roles/UseCases/UpdateRoleCommandHandler.cs`
- Modify: `src/Application/Roles/UseCases/DeleteRoleCommandHandler.cs`
- Modify: `src/Application/Users/UseCases/CreateUserCommandHandler.cs`
- Modify: `src/Application/Users/UseCases/UpdateUserCommandHandler.cs`
- Modify: `src/Application/Users/UseCases/DeactivateUserCommandHandler.cs`
- Modify: `src/Application/UserAccounts/UseCase/UpdateUserAccountCommandHandler.cs`
- Modify: `src/Application/UserAccounts/UseCase/DeleteUserAccountCommandHandler.cs`
- Create: `tests/Application.Tests/Roles/SystemRoleProtectionTests.cs`
- Create: `tests/Application.Tests/Users/SuperAdminUserProtectionTests.cs`
- Create: `tests/Application.Tests/UserAccounts/SuperAdminAccountProtectionTests.cs`

**Interfaces:**
- Consumes: `SystemRoles` from Task 1 and existing `IUnitOfWork` repositories.
- Produces: application-level rejection through the existing `ForbiddenException` for protected lifecycle operations.

- [ ] **Step 1: Write failing handler tests for forbidden role operations**

```csharp
[Fact]
public async Task Delete_rejects_the_canonical_SuperAdmin_role()
{
    unitOfWork.RolesRepository.GetByIdAsync(SystemRoles.SuperAdminId, token)
        .Returns(new Roles(SystemRoles.SuperAdminName, "Rol de sistema"));

    await Assert.ThrowsAsync<ForbiddenException>(() =>
        handler.Handle(new DeleteRoleCommand(SystemRoles.SuperAdminId), token));
}
```

Cover create-by-reserved-name, rename, delete, user creation with the canonical role, promotion to the canonical role, demotion/deactivation of a user already holding it, and update/delete of that user's account.

- [ ] **Step 2: Run only the three new protection test classes**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~SystemRoleProtectionTests|FullyQualifiedName~SuperAdminUserProtectionTests|FullyQualifiedName~SuperAdminAccountProtectionTests"`

Expected: failures showing the current handlers permit the protected operations.

- [ ] **Step 3: Add minimal guards before mutations**

Use `SystemRoles.IsReservedName` in role creation and `SystemRoles.IsSuperAdmin` for canonical IDs/current role IDs. Resolve the account's related user before account update/delete. Throw stable Spanish `ForbiddenException` messages without exposing internal data.

```csharp
if (SystemRoles.IsSuperAdmin(role.Id))
{
    throw new ForbiddenException("El rol SuperAdmin es administrado mediante el proceso seguro de aprovisionamiento.");
}
```

- [ ] **Step 4: Run protection and existing user/account tests**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~SystemRoleProtectionTests|FullyQualifiedName~SuperAdminUserProtectionTests|FullyQualifiedName~SuperAdminAccountProtectionTests|FullyQualifiedName~ActivateUserCommandHandlerTests|FullyQualifiedName~DeactivateUserCommandHandlerTests|FullyQualifiedName~UserAccount"`

Expected: selected tests pass without changing ordinary Administrator behavior.

- [ ] **Step 5: Commit application protections**

```bash
git add src/Application tests/Application.Tests
git commit -m "fix(security): 🐛 protect SuperAdmin lifecycle operations"
```

---

### Task 3: Unify persisted authentication and token issuance

**Files:**
- Modify: `src/Infrastructure/Security/Authentication/AuthenticationService.cs`
- Modify: `src/Infrastructure/Security/Authentication/ClientAccountRegistrationService.cs`
- Modify: `src/Infrastructure/Security/Tokens/JwtTokenIssuer.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Delete: `src/Infrastructure/Security/Options/SuperAdminOptions.cs`
- Delete: `src/Infrastructure/Security/Options/SuperAdminOptionsValidator.cs`
- Delete: `src/Infrastructure/Security/Options/BootstrapAdminOptions.cs`
- Delete: `src/Infrastructure/Security/Options/BootstrapAdminOptionsValidator.cs`
- Replace: `tests/Api.Tests/Security/AuthenticationServiceSuperAdminTests.cs`
- Modify: `tests/Api.Tests/Security/JwtTokenIssuerTests.cs`
- Modify: `tests/Api.Tests/Security/ClientAccountRegistrationServiceTests.cs`
- Delete: `tests/Api.Tests/Security/SuperAdminOptionsValidatorTests.cs`
- Modify: `tests/Api.Tests/Security/SecurityStage1Tests.cs`

**Interfaces:**
- Consumes: existing persisted repositories and `AuthenticatedIdentity`.
- Produces: normal access/refresh tokens and `/me` identity for SuperAdmin accounts; no configuration-backed identity path.

- [ ] **Step 1: Rewrite focused tests around a persisted SuperAdmin account**

```csharp
[Fact]
public async Task Login_for_persisted_SuperAdmin_issues_access_and_refresh_tokens()
{
    // Arrange normal USERS, USER_ACCOUNTS and USER_CREDENTIALS rows whose role is SystemRoles.SuperAdminId.
    var result = await service.LoginAsync("root@huellitas.test", "ValidPassword!1", token);

    Assert.True(result.IsSuccess);
    Assert.NotEmpty(result.Value.RefreshToken);
}
```

Assert that the JWT has `sub`, `person_id`, `role_id`, `role` and email, and does not require `super_admin=true`.

- [ ] **Step 2: Run the focused authentication tests and observe failures**

Run: `dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~AuthenticationServiceSuperAdminTests|FullyQualifiedName~JwtTokenIssuerTests|FullyQualifiedName~ClientAccountRegistrationServiceTests|FullyQualifiedName~SecurityStage1Tests"`

Expected: failures because the synthetic option path and special token issuer still exist.

- [ ] **Step 3: Remove the synthetic branches and obsolete options**

Make `LoginAsync`, `RefreshAsync`, `GetCurrentProfileAsync` and token issuance use the normal persisted identity path exclusively. Remove `IssueForSuperAdmin`, `BuildSuperAdminProfile`, special email checks and option registration. Remove the unused `BootstrapAdminOptions` types because they have no runtime registration or consumer and would preserve a misleading credential-from-configuration pattern.

- [ ] **Step 4: Run the focused authentication tests**

Run: `dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~AuthenticationServiceSuperAdminTests|FullyQualifiedName~JwtTokenIssuerTests|FullyQualifiedName~ClientAccountRegistrationServiceTests|FullyQualifiedName~SecurityStage1Tests"`

Expected: selected tests pass and persisted SuperAdmin receives a refresh token.

- [ ] **Step 5: Commit authentication unification**

```bash
git add src/Infrastructure tests/Api.Tests/Security
git commit -m "fix(auth): 🐛 persist SuperAdmin authentication lifecycle"
```

---

### Task 4: Replace special-claim authorization with canonical role authorization

**Files:**
- Create: `src/Api/Common/Security/ClaimsPrincipalExtensions.cs`
- Modify: `src/Api/Common/Security/Permissions/PermissionAuthorizationHandler.cs`
- Modify: `src/Api/Extensions/AuthorizationExtensions.cs`
- Modify: `src/Api/Auth/Controllers/AuthControllers.cs`
- Modify: `src/Api/Appointments/Controllers/AppointmentsController.cs`
- Modify: `tests/Api.Tests/Security/AuthorizationPoliciesTests.cs`
- Modify: `tests/Api.Tests/Security/PermissionAuthorizationHandlerTests.cs`
- Modify: `tests/Api.Tests/Security/AuthControllerPermissionsTests.cs`
- Modify: `tests/Api.Tests/Security/RolesAndDiagnosticsAuthorizationTests.cs`
- Modify: `tests/Api.Tests/Appointments/AppointmentOwnershipApiTests.cs`
- Modify: `tests/Api.Tests/Appointments/AppointmentMedicalRecordApiTests.cs`

**Interfaces:**
- Consumes: the JWT `role_id` claim and `SystemRoles.SuperAdminId`.
- Produces: `ClaimsPrincipal.IsSuperAdmin()` as the single authorization interpretation.

- [ ] **Step 1: Change policy tests to use a normal persisted-role principal**

```csharp
private static ClaimsPrincipal SuperAdminPrincipal() =>
    PrincipalWithClaims(
        new Claim("role_id", SystemRoles.SuperAdminId.ToString()),
        new Claim("role", SystemRoles.SuperAdminName));
```

Assert that `SuperAdminOnly`, role policies, dynamic permissions, permissions listing and appointment ownership bypass accept this principal and reject a forged `super_admin=true` claim without the canonical `role_id`.

- [ ] **Step 2: Run only affected API authorization tests**

Run: `dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~AuthorizationPoliciesTests|FullyQualifiedName~PermissionAuthorizationHandlerTests|FullyQualifiedName~AuthControllerPermissionsTests|FullyQualifiedName~RolesAndDiagnosticsAuthorizationTests|FullyQualifiedName~AppointmentOwnershipApiTests|FullyQualifiedName~AppointmentMedicalRecordApiTests"`

Expected: failures where production code still checks the obsolete claim.

- [ ] **Step 3: Centralize and replace authorization checks**

```csharp
public static bool IsSuperAdmin(this ClaimsPrincipal principal) =>
    Guid.TryParse(principal.FindFirst("role_id")?.Value, out var roleId) &&
    SystemRoles.IsSuperAdmin(roleId);
```

Use the extension in `SuperAdminOnly`, `RoleOrSuperAdmin`, permission bypass, effective-permissions response and appointment ownership. Do not authorize by display name alone.

- [ ] **Step 4: Run the focused authorization tests**

Run the Task 4 filter again.

Expected: all selected tests pass; forged legacy claims are rejected.

- [ ] **Step 5: Commit API authorization changes**

```bash
git add src/Api tests/Api.Tests
git commit -m "fix(authorization): 🐛 resolve SuperAdmin from persisted role"
```

---

### Task 5: Build complete idempotent production catalog seeds

**Files:**
- Modify: `database/seeds/roles_seed.sql`
- Modify: `database/seeds/modules_seed.sql`
- Modify: `database/seeds/role_permissions_seed.sql`
- Modify: `database/seeds/status_appointments_seed.sql`
- Replace: `database/seeds/chat_conversation_catalogs_seed.sql`
- Create: `database/seeds/chat_runtime_catalogs_seed.sql`
- Create: `database/seeds/veterinary_catalogs_seed.sql`
- Create: `database/seeds/apply_all.sql`
- Create: `database/seeds/verify_seeds.sql`
- Create: `database/admin/promote_superadmin.sql`
- Modify: `database/seeds/README.md`

**Interfaces:**
- Consumes: existing Oracle table/column names and the canonical SuperAdmin GUID from Task 1.
- Produces: repeatable catalog state and an explicit, parameterized account-promotion operation.

- [ ] **Step 1: Define the exact canonical rows in SQL comments and verification expectations**

Use these minimum sets:

- Roles: SuperAdmin, Administrador, Veterinario, Recepcionista, Auxiliar, Cliente.
- Appointment statuses: AGENDADA, CONFIRMADA, EN_PROGRESO, ATENDIDA, CANCELADA, NO_ASISTIO.
- Conversation statuses: Abierta, En atención, Escalada, Cerrada.
- Sender types: Cliente, Agente IA, Agente humano, Sistema.
- Message types: Texto, Imagen, Audio, Documento, Sistema.
- Priorities: Baja, Media, Alta, Urgente.
- Escalation statuses: Pendiente, Asignada, En atención, Resuelta, Cancelada.
- AI run statuses: Pendiente, En ejecución, Completada, Fallida, Cancelada.
- Type services: Consulta, Vacunación, Procedimiento, Diagnóstico, Urgencia.
- Species: Perro, Gato, Otro.
- Specialties: Medicina general, Cirugía, Dermatología, Medicina interna, Urgencias.

- [ ] **Step 2: Convert existing raw inserts to idempotent MERGE statements**

Every statement must preserve referential consistency, avoid duplicate canonical names and use the actual Oracle column names from EF configurations. `role_permissions_seed.sql` must be safely executable twice.

```sql
MERGE INTO ROLES target
USING (
    SELECT '99999999-9999-9999-9999-999999999999' ROLE_ID,
           'SuperAdmin' NAME,
           'Rol de sistema con autoridad no delegable' DESCRIPTION
    FROM DUAL
) source
ON (target.ROLE_ID = source.ROLE_ID)
WHEN MATCHED THEN UPDATE SET target.DESCRIPTION = source.DESCRIPTION
WHEN NOT MATCHED THEN
    INSERT (ROLE_ID, NAME, DESCRIPTION, CREATED_AT)
    VALUES (source.ROLE_ID, source.NAME, source.DESCRIPTION, SYSTIMESTAMP);
```

- [ ] **Step 3: Add ordered execution, verification and secure promotion**

`apply_all.sql` invokes only non-destructive production seeds. `verify_seeds.sql` reports each expected catalog row and conflicting canonical IDs. `promote_superadmin.sql` accepts `&1` as email, validates one active credentialed account, updates its user's role and deletes existing refresh tokens in one transaction; it rolls back and raises an Oracle application error when a precondition fails.

- [ ] **Step 4: Perform static seed checks without touching Oracle**

Run:

```powershell
rg -n "INSERT INTO ROLE_PERMISSIONS" database/seeds/role_permissions_seed.sql
rg -n "cleanup_seeds" database/seeds/apply_all.sql
rg -n "Password|PasswordHash|@gmail|@hotmail" database/seeds database/admin
```

Expected: permissions are expressed through `MERGE`; the normal runner does not invoke cleanup; no personal credential or email is present.

- [ ] **Step 5: Commit database initialization assets**

```bash
git add database/seeds database/admin
git commit -m "fix(seeds): 🐛 complete idempotent operational catalogs"
```

---

### Task 6: Remove obsolete configuration and document rollout

**Files:**
- Modify: `.env.example`
- Modify: `docs/CONTEXT_REVISION_BACKEND.md`
- Modify: `database/seeds/README.md`
- Create: `docs/SUPERADMIN_PROVISIONING.md`

**Interfaces:**
- Consumes: final runtime and database behavior from Tasks 1–5.
- Produces: exact installation, promotion, login and rollback instructions without secrets.

- [ ] **Step 1: Remove obsolete environment examples and document the transition**

Delete every `SuperAdmin__*` example. Explain that JWT RSA variables remain required, while SuperAdmin identity now comes from Oracle. Document seed order, SQL*Plus invocation, promotion by email, forced relogin and how to verify `/api/auth/me` and `/api/auth/permissions`.

- [ ] **Step 2: Scan runtime and docs for obsolete behavior**

Run: `rg -n "SuperAdminOptions|SuperAdmin__|IssueForSuperAdmin|super_admin" src tests .env.example`

Expected: no runtime/configuration reference remains. Historical design documents may describe the replaced state but must clearly label it as historical.

- [ ] **Step 3: Run the focused regression set and build**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~SystemRole|FullyQualifiedName~SuperAdmin|FullyQualifiedName~UserAccount|FullyQualifiedName~UserCommand"`

Run: `dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~AuthenticationServiceSuperAdminTests|FullyQualifiedName~JwtTokenIssuerTests|FullyQualifiedName~AuthorizationPoliciesTests|FullyQualifiedName~PermissionAuthorizationHandlerTests|FullyQualifiedName~AuthControllerPermissionsTests|FullyQualifiedName~RolesAndDiagnosticsAuthorizationTests"`

Run: `dotnet build veterinarian_backend.slnx --no-restore`

Expected: all selected tests pass; build has 0 warnings and 0 errors.

- [ ] **Step 4: Perform final compatibility and hygiene checks**

Run: `git diff --check`

Run: `git status --short`

Review: login/refresh/profile DTOs unchanged, no EF migration generated, no Oracle command executed, no chatbot files included and no secrets in the diff.

- [ ] **Step 5: Commit rollout documentation**

```bash
git add .env.example docs database/seeds/README.md
git commit -m "docs(security): 📝 document persisted SuperAdmin provisioning"
```

## Rollout and Forward-Fix Strategy

1. Deploy code and seeds together during a controlled maintenance window.
2. Run `apply_all.sql` against the explicitly selected Oracle schema.
3. Promote one existing active internal account with `promote_superadmin.sql`.
4. Revoke old sessions and log in again to obtain a canonical-role JWT.
5. Verify `/api/auth/me`, `/api/auth/permissions` and one `SuperAdminOnly` endpoint.
6. If provisioning fails, the script rolls back; correct the missing account/credential/catalog prerequisite and rerun it.
7. If application rollout must be reversed, revert the application deployment. The added role/catalog rows are additive and may remain; no destructive schema rollback is necessary.

