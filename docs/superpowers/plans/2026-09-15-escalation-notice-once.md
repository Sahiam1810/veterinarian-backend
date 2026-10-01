# Single Escalation Notice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enviar la confirmación de escalamiento una sola vez y completar silenciosamente los mensajes posteriores mientras un asesor controla la conversación.

**Architecture:** `TelegramInboundUpdate` tendrá una transición de dominio para finalizar un update en procesamiento sin respuesta. `ProcessTelegramUpdateHandler` persistirá primero el mensaje del cliente y, si el contexto ya está escalado, usará esa transición y terminará antes del detector de escalamiento y del dispatcher del chatbot.

**Tech Stack:** .NET 10, C#, MediatR, EF Core, xUnit, NSubstitute.

## Global Constraints

- Rama: `fix/escalation-notice-once`, creada desde `develop` actualizado.
- El primer escalamiento conserva su confirmación actual.
- Los mensajes posteriores siguen persistiendo para el asesor.
- No invocar chatbot, IA ni crear otro escalamiento mientras `IsEscalated` sea verdadero.
- Sin cambios de esquema, migraciones, seeders, endpoints o autorización.
- Aplicar TDD y detenerse para verificación entre Domain y Application.

---

## Baseline

- Target module: Telegram processing.
- Existing related use case: `ProcessTelegramUpdateCommand`.
- Git state: rama nueva y limpia antes de documentar el plan.
- Relevant baseline: 36 pruebas de `ProcessTelegramUpdateHandlerTests` y `TelegramEntitiesTests` correctas.
- Known global baseline: dos pruebas de API de seeders fallan en `develop`; quedan fuera de este cambio.

## Approved Use Case Contract

- Business objective: evitar notificaciones automáticas repetidas durante un escalamiento activo.
- Trigger: update interno recibido desde Telegram.
- Input: `AgentConversationContext.IsEscalated` resuelto desde la base de datos.
- Output: primer escalamiento con confirmación; turnos posteriores completados sin respuesta.
- Authorization: conserva el flujo interno existente; no cambia políticas.
- Idempotency: un update completado no se vuelve a procesar; no se duplica el escalamiento.
- Side effects: siempre persistir el mensaje del cliente antes de completar silenciosamente.
- Errors: una falla de persistencia conserva el retry actual; no completar prematuramente.
- Endpoint: ninguno.
- Compatibility: no cambia el contrato HTTP ni la estructura persistida.

## Layer impact

| Layer | Required? | Existing files changed | New files | Reason |
|---|---:|---|---|---|
| Domain | yes | `src/Domain/Telegram/Entities/TelegramInboundUpdate.cs` | none | Nueva transición de ciclo de vida sin respuesta. |
| Application | yes | `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs` | none | Corte temprano cuando el contexto ya está escalado. |
| Infrastructure | no | none | none | EF persiste propiedades existentes; no hay nueva consulta o esquema. |
| Api | no | none | none | No cambia ningún endpoint o contrato. |

---

### Task 1: Completar un update sin respuesta

**Files:**
- Modify: `src/Domain/Telegram/Entities/TelegramInboundUpdate.cs`
- Test: `tests/Application.Tests/Telegram/Domain/TelegramEntitiesTests.cs`

**Interfaces:**
- Consumes: update con `Status == TelegramInboundUpdateStatus.Processing`.
- Produces: `void CompleteWithoutResponse(DateTime completedAt)`.

- [x] **Step 1: Escribir pruebas fallidas de la transición**

Agregar una prueba que cree y reclame un update, invoque:

```csharp
update.CompleteWithoutResponse(Now.AddSeconds(1));

Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
Assert.Null(update.MessageText);
Assert.Null(update.ResponseText);
Assert.Null(update.LastErrorCode);
Assert.Equal(Now.AddSeconds(1), update.UpdatedAt);
```

Agregar otra prueba que invoque el método antes de `Claim` y espere
`InvalidOperationException`.

- [x] **Step 2: Ejecutar RED**

Run:

```powershell
dotnet test tests\Application.Tests\Application.Tests.csproj --filter "FullyQualifiedName~TelegramEntitiesTests" -p:RestorePackagesPath=.cache\nuget --no-restore --verbosity minimal
```

Expected: FAIL de compilación porque `CompleteWithoutResponse` todavía no existe.

- [x] **Step 3: Implementar la transición mínima**

Agregar a `TelegramInboundUpdate`:

```csharp
public void CompleteWithoutResponse(DateTime completedAt)
{
    if (Status != TelegramInboundUpdateStatus.Processing)
    {
        throw new InvalidOperationException(
            "Solo una actualización en procesamiento puede completarse sin respuesta.");
    }

    Status = TelegramInboundUpdateStatus.Completed;
    MessageText = null;
    ResponseText = null;
    LastErrorCode = null;
    UpdatedAt = completedAt;
}
```

- [x] **Step 4: Ejecutar GREEN y verificar Domain**

Ejecutar la prueba focalizada anterior y:

```powershell
dotnet build src\Domain\Domain.csproj -p:RestorePackagesPath=.cache\nuget --no-restore --verbosity minimal
```

Expected: todas las pruebas focalizadas pasan y Domain compila sin errores.

- [x] **Step 5: Detenerse en el gate de Domain**

Revisar que no haya atributos nuevos, dependencias externas ni cambios de EF. Solicitar aprobación antes de Application.

---

### Task 2: Silenciar turnos posteriores del escalamiento

**Files:**
- Modify: `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs`
- Test: `tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs`

**Interfaces:**
- Consumes: `AgentConversationContext.IsEscalated` y `TelegramInboundUpdate.CompleteWithoutResponse(DateTime)`.
- Produces: mensaje persistido y update completado sin llamada al dispatcher, creación de escalamiento o envío a Telegram.

- [x] **Step 1: Escribir la prueba fallida del comportamiento observable**

Preparar un update vinculado cuyo contexto sea:

```csharp
new AgentConversationContext(ConversationId, "web", true)
```

Usar un mensaje normal como `Necesito agregar otro detalle` y afirmar:

```csharp
await fixture.Sender.Received(1).Send(
    Arg.Is<CreateChatMessageCommand>(command =>
        command.ChatConversationId == ConversationId &&
        command.Content == message),
    default);
await fixture.Dispatcher.DidNotReceive().DispatchAsync(
    Arg.Any<AgentMessageDispatchRequest>(),
    Arg.Any<AgentConversationContext>(),
    Arg.Any<string>(),
    Arg.Any<CancellationToken>());
await fixture.Sender.DidNotReceive().Send(
    Arg.Any<CreateChatEscalationCommand>(),
    Arg.Any<CancellationToken>());
await fixture.Bot.DidNotReceive().SendTextAsync(
    Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
```

Convertir la prueba existente de frase repetida para afirmar igualmente que no se envía la confirmación.

- [x] **Step 2: Ejecutar RED**

Run:

```powershell
dotnet test tests\Application.Tests\Application.Tests.csproj --filter "FullyQualifiedName~ProcessTelegramUpdateHandlerTests" -p:RestorePackagesPath=.cache\nuget --no-restore --verbosity minimal
```

Expected: FAIL porque el handler llama al dispatcher o envía nuevamente la confirmación.

- [x] **Step 3: Implementar el corte temprano**

Después de `PersistClientMessageAsync(...)` y antes de `IsEscalationRequest(...)`, agregar:

```csharp
if (context.IsEscalated)
{
    await CompleteWithoutResponseAsync(update, cancellationToken);
    return;
}
```

Agregar el helper:

```csharp
private async Task CompleteWithoutResponseAsync(
    TelegramInboundUpdate update,
    CancellationToken cancellationToken)
{
    update.CompleteWithoutResponse(timeProvider.GetUtcNow().UtcDateTime);
    await unitOfWork.InboundUpdatesRepository.UpdateAsync(update, cancellationToken);
    await unitOfWork.SaveChangesAsync(cancellationToken);
}
```

- [x] **Step 4: Ejecutar GREEN y regresiones focalizadas**

Run:

```powershell
dotnet test tests\Application.Tests\Application.Tests.csproj --filter "FullyQualifiedName~ProcessTelegramUpdateHandlerTests|FullyQualifiedName~TelegramEntitiesTests" -p:RestorePackagesPath=.cache\nuget --no-restore --verbosity minimal
```

Expected: todas las pruebas pasan; la prueba de primer escalamiento sigue enviando exactamente una confirmación.

- [x] **Step 5: Verificar Application y detenerse**

Run:

```powershell
dotnet test tests\Application.Tests\Application.Tests.csproj -p:RestorePackagesPath=.cache\nuget --no-restore --verbosity minimal
```

Expected: Application verde. Revisar propagación de `CancellationToken`, orden de persistencia y ausencia de referencias a Infrastructure/Api.

---

### Task 3: Auditoría final y commit

**Files:**
- Verify: todos los archivos modificados de Tasks 1 y 2.

**Interfaces:**
- Consumes: implementación completa y baseline registrado.
- Produces: rama verificable y commit local, sin push.

- [x] **Step 1: Ejecutar compilación y suites disponibles**

```powershell
dotnet build veterinarian_backend.slnx -p:RestorePackagesPath=.cache\nuget --no-restore --verbosity minimal
dotnet test veterinarian_backend.slnx -p:RestorePackagesPath=.cache\nuget --no-restore --no-build --verbosity minimal
```

Comparar cualquier fallo global con los dos fallos de seeders registrados en el baseline.

- [x] **Step 2: Revisar compatibilidad y diff**

```powershell
git diff --check develop...HEAD
git status --short --branch
git diff --name-only develop...HEAD
```

Confirmar que no existen cambios de API, configuración, migraciones, seeders o secretos.

- [x] **Step 3: Crear commit de implementación**

```powershell
git add src/Domain/Telegram/Entities/TelegramInboundUpdate.cs tests/Application.Tests/Telegram/Domain/TelegramEntitiesTests.cs src/Application/Telegram/Processing/ProcessTelegramUpdate.cs tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs docs/superpowers/plans/2026-09-15-escalation-notice-once.md
git commit -m "fix(telegram): send escalation notice once"
```

## Final verification

- Full build/tests: solución completa; comparar fallos con baseline.
- Endpoint/OpenAPI: no aplica, no hay cambio HTTP.
- Authorization: no cambia; no se añade `AuthManagementGrant`.
- Idempotency: un update finaliza una vez y no se vuelve a reclamar.
- Regression: primer aviso, mensaje escalado normal, frase de escalamiento repetida y flujo no escalado.
- Remaining risks: ninguno conocido fuera de los fallos preexistentes de seeders.
