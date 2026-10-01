# Telegram Stale Identity Access Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recuperar automáticamente un chat con una vinculación obsoleta solicitando cédula y conservando el flujo existente de OTP y registro mínimo.

**Architecture:** La recuperación del enlace pertenece a Application y reutiliza `TelegramUserLink.Revoke`. La resolución de identidad en Infrastructure reconoce cualquier rol cuando existen cliente, usuario y cuenta activos; el JWT conserva el rol real. Un error específico separa conflictos de registro de conflictos de vinculación, sin modificar Api ni el esquema de Oracle.

**Tech Stack:** .NET 10, C#, MediatR application layer, NSubstitute, xUnit.

## Global Constraints

- No usar ni mostrar `/vincular` o `/registrar` en el flujo corregido.
- No eliminar físicamente una vinculación: revocarla para conservar trazabilidad.
- No exponer cédula, correo, OTP o mensaje privado en logs.
- No cambiar el rol de usuarios existentes ni crear permisos especiales para Telegram.
- No crear migraciones ni modificar contratos HTTP.
- Ejecutar solamente las pruebas enfocadas de identidad de Telegram y la compilación final.

---

### Task 1: Recuperar una vinculación que ya no tiene perfil de cliente activo

**Files:**
- Modify: `tests/Application.Tests/Telegram/TelegramIdentityAccessServiceTests.cs`
- Modify: `src/Application/Telegram/Identity/TelegramIdentityAccessService.cs`

**Interfaces:**
- Consumes: `ITelegramUserLinkRepository.GetByTelegramUserIdAsync`, `ITelegramClientIdentityGateway.FindActiveByPersonIdAsync`, `TelegramUserLink.Revoke`, `ITelegramIdentitySessionRepository.AddAsync`, `ITelegramUnitOfWork.SaveChangesAsync`.
- Produces: el comportamiento de `BeginPrivateAccessAsync` que convierte un enlace obsoleto en una sesión `AwaitingIdentification` y devuelve una solicitud de cédula.

- [ ] **Step 1: Escribir la prueba de regresión**

Agregar a `TelegramIdentityAccessServiceTests`:

```csharp
[Fact]
public async Task Stale_link_is_revoked_and_private_access_requests_identification()
{
    var fixture = CreateFixture();
    var update = ProcessingUpdate(42, "quiero agendar una cita");
    var staleLink = TelegramUserLink.Create(
        PersonId,
        1001,
        1001,
        Now.AddDays(-1).UtcDateTime);
    fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(staleLink);
    fixture.Clients.FindActiveByPersonIdAsync(PersonId, default)
        .Returns((TelegramClientIdentity?)null);

    var outcome = await fixture.Service.BeginPrivateAccessAsync(update, default);

    Assert.True(outcome.Consumed);
    Assert.Contains("cédula", outcome.Reply!, StringComparison.OrdinalIgnoreCase);
    Assert.False(staleLink.IsActive);
    await fixture.UserLinks.Received(1).UpdateAsync(staleLink, default);
    await fixture.Sessions.Received(1).AddAsync(
        Arg.Is<TelegramIdentitySession>(session =>
            session.Status == TelegramIdentitySessionStatus.AwaitingIdentification &&
            session.PendingInboundUpdateId == 42),
        default);
    await fixture.UnitOfWork.Received(1).SaveChangesAsync(default);
}
```

- [ ] **Step 2: Ejecutar la prueba y comprobar el fallo esperado**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~Stale_link_is_revoked_and_private_access_requests_identification" -v:minimal
```

Expected: FAIL porque la respuesta actual indica que el perfil no está disponible y el enlace continúa activo.

- [ ] **Step 3: Implementar la recuperación mínima**

En la rama `identity is null` de `BeginPrivateAccessAsync`, reemplazar la respuesta terminal por:

```csharp
link.Revoke(now.UtcDateTime);
await unitOfWork.UserLinksRepository.UpdateAsync(link, cancellationToken);
await unitOfWork.IdentitySessionsRepository.AddAsync(session, cancellationToken);
await unitOfWork.SaveChangesAsync(cancellationToken);
return new TelegramIdentityAccessOutcome(
    true,
    "Tu acceso anterior ya no corresponde a un perfil activo. Para proteger tus datos, escribe tu número de cédula. Puedes usar /cancelar para salir.");
```

- [ ] **Step 4: Ejecutar las pruebas enfocadas**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramIdentityAccessServiceTests" -v:minimal
```

Expected: PASS para las 7 pruebas del servicio de identidad.

- [ ] **Step 5: Verificar compilación y diff**

Run:

```powershell
dotnet build --no-restore -v:minimal
git diff --check
```

Expected: compilación con 0 errores y 0 advertencias; `git diff --check` sin errores.

- [ ] **Step 6: Crear el commit funcional**

```powershell
git add src/Application/Telegram/Identity/TelegramIdentityAccessService.cs tests/Application.Tests/Telegram/TelegramIdentityAccessServiceTests.cs
git commit -m "fix(telegram): 🐛 recover stale identity access"
```

### Task 2: Resolver clientes activos independientemente de su rol

**Files:**
- Modify: `tests/Infrastructure.Tests/Telegram/TelegramClientIdentityGatewayTests.cs`
- Modify: `src/Infrastructure/Telegram/Identity/TelegramClientIdentityGateway.cs`

**Interfaces:**
- Consumes: `IClientRepository`, `IUsersRepository` y `IUserAccountsRepository`.
- Produces: `FindActiveByIdentificationAsync` y `FindActiveByPersonIdAsync` resuelven una identidad cuando existen cliente, usuario activo y cuenta activa, sin usar el rol como condición de identidad.

- [ ] **Step 1: Agregar una prueba con rol SuperAdmin**

Agregar a `TelegramClientIdentityGatewayTests`:

```csharp
[Fact]
public async Task Active_client_with_administrative_role_is_resolved_by_identification()
{
    var clients = Substitute.For<IClientRepository>();
    var users = Substitute.For<IUsersRepository>();
    var accounts = Substitute.For<IUserAccountsRepository>();
    var roles = Substitute.For<IRolesRepository>();
    var role = new RoleEntity("SuperAdmin", null);
    var user = new UserEntity("Ana Pérez", "ana@example.test", null, role.Id);
    var account = new UserAccountEntity(user.Id, "admin_ana", "ana@example.test", "Activo");
    var client = new ClientEntity(user.Id, "123456789", null);
    clients.GetByIdentificationNumberAsync("123456789", default).Returns(client);
    users.GetByIdAsync(user.Id, default).Returns(user);
    accounts.GetByUserIdAsync(user.Id, default).Returns(account);
    roles.GetByIdAsync(role.Id, default).Returns(role);
    var gateway = new TelegramClientIdentityGateway(clients, users, accounts, roles);

    var result = await gateway.FindActiveByIdentificationAsync("123456789", default);

    Assert.Equal(new TelegramClientIdentity(user.Id, account.Id, "ana@example.test"), result);
}
```

- [ ] **Step 2: Verificar que la prueba falla**

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~Active_client_with_administrative_role_is_resolved_by_identification" -v:minimal
```

Expected: FAIL porque `ResolveActiveAsync` exige actualmente el nombre de rol `Cliente`.

- [ ] **Step 3: Eliminar la comprobación de rol de `ResolveActiveAsync`**

Conservar las validaciones de usuario activo y cuenta con estado `Activo`; el repositorio de clientes ya demuestra que la persona tiene perfil de cliente. Mantener `IRolesRepository` únicamente para asignar el rol `Cliente` durante registros realmente nuevos.

- [ ] **Step 4: Verificar el gateway**

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramClientIdentityGatewayTests" -v:minimal
```

Expected: PASS para las pruebas enfocadas del gateway.

### Task 3: Responder de forma controlada a duplicados residuales

**Files:**
- Modify: `src/Application/Telegram/Errors/TelegramExceptions.cs`
- Modify: `src/Infrastructure/Telegram/Identity/TelegramClientIdentityGateway.cs`
- Modify: `src/Application/Telegram/Identity/TelegramIdentityAccessService.cs`
- Modify: `tests/Application.Tests/Telegram/TelegramIdentityAccessServiceTests.cs`

**Interfaces:**
- Produces: `TelegramRegistrationConflictException`, emitida solamente antes de agregar entidades nuevas y manejada por `ProcessOtpAsync`.

- [ ] **Step 1: Agregar la regresión de conflicto posterior al OTP**

Agregar a `TelegramIdentityAccessServiceTests`:

```csharp
[Fact]
public async Task Registration_conflict_after_valid_otp_returns_controlled_reply()
{
    var fixture = CreateFixture();
    var session = RegistrationOtpSession();
    fixture.Sessions.GetCurrentByTelegramUserIdAsync(1001, default).Returns(session);
    fixture.Otp.Verify("123456", Hash).Returns(true);
    fixture.Clients.StageRegistrationAsync(
            Arg.Any<TelegramClientRegistration>(),
            default)
        .Returns<Task<TelegramClientIdentity>>(_ =>
            throw new TelegramRegistrationConflictException());

    var outcome = await fixture.Service.HandleActiveFlowAsync(
        ProcessingUpdate(47, "123456"), default);

    Assert.True(outcome.Consumed);
    Assert.Contains("ya corresponden a una cuenta", outcome.Reply!,
        StringComparison.OrdinalIgnoreCase);
    Assert.Equal(TelegramIdentitySessionStatus.Cancelled, session.Status);
}
```

- [ ] **Step 2: Verificar que la prueba falla**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~Registration_conflict_after_valid_otp_returns_controlled_reply" -v:minimal
```

Expected: FAIL porque el error todavía se propaga al worker.

- [ ] **Step 3: Implementar el error específico y su manejo**

Declarar `TelegramRegistrationConflictException` en Application, emitirlo desde la validación de duplicados de `StageRegistrationAsync` y capturarlo alrededor de la transacción en `ProcessOtpAsync`. Cancelar y persistir la sesión, y devolver un mensaje controlado para reiniciar con la cédula correcta o solicitar soporte.

```csharp
public sealed class TelegramRegistrationConflictException()
    : TelegramIntegrationException("The Telegram registration data already exists.");
```

La captura se limita a este tipo específico:

```csharp
catch (TelegramRegistrationConflictException)
{
    session.Cancel(now.UtcDateTime);
    await PersistSessionAsync(session, cancellationToken);
    return new TelegramIdentityAccessOutcome(
        true,
        "Los datos ingresados ya corresponden a una cuenta de Huellitas, pero no pudimos habilitarla para Telegram. Repite la solicitud con la cédula correcta o solicita soporte.");
}
```

- [ ] **Step 4: Verificación final enfocada**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramIdentityAccessServiceTests" -v:minimal
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramClientIdentityGatewayTests" -v:minimal
dotnet build -c Release --no-restore -v:minimal
git diff --check
```

Expected: todas las pruebas enfocadas pasan; compilación con 0 errores y 0 advertencias; diff limpio.

## Self-review

- Cobertura del diseño: enlace obsoleto; cliente con rol administrativo; OTP y registro nuevo; conflicto residual controlado; mensaje original preservado.
- Placeholders: ninguno.
- Consistencia de tipos: utiliza las firmas actuales y agrega solamente un error específico de Application.
- Capas no afectadas: Domain y Api no requieren cambios; no existe impacto de esquema ni migración.
