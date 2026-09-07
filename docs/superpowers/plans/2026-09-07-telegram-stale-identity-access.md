# Telegram Stale Identity Access Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recuperar automáticamente un chat con una vinculación obsoleta solicitando cédula y conservando el flujo existente de OTP y registro mínimo.

**Architecture:** La corrección pertenece al caso de uso de acceso privado de Telegram en Application. Reutiliza `TelegramUserLink.Revoke`, los repositorios actuales y una única unidad de persistencia; no modifica Domain, Infrastructure, Api ni el esquema de Oracle.

**Tech Stack:** .NET 10, C#, MediatR application layer, NSubstitute, xUnit.

## Global Constraints

- No usar ni mostrar `/vincular` o `/registrar` en el flujo corregido.
- No eliminar físicamente una vinculación: revocarla para conservar trazabilidad.
- No exponer cédula, correo, OTP o mensaje privado en logs.
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

## Self-review

- Cobertura del diseño: enlace válido sin cambios; enlace ausente sin cambios; enlace obsoleto revocado; cédula/OTP/registro posterior reutilizado; mensaje original preservado.
- Placeholders: ninguno.
- Consistencia de tipos: utiliza las firmas y entidades existentes sin crear puertos nuevos.
- Capas no afectadas: Domain, Infrastructure y Api no requieren cambios.
