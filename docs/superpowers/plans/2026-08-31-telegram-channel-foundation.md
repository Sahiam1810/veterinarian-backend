# Telegram Channel Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recibir texto desde chats privados de Telegram, vincularlo de forma segura con una cuenta Huellitas, crear o reutilizar su conversación y participante, solicitar una respuesta al agente y devolverla a Telegram sin persistir historial en `CHAT_MESSAGES`.

**Architecture:** El módulo `Telegram` actúa como adaptador de entrada/salida y mantiene vínculos e inbox técnicos en Oracle. El webhook confirma rápidamente y un `BackgroundService` procesa los trabajos; la coordinación común del módulo `Agent` se extrae a un dispatcher que sirve tanto al endpoint JWT existente como al worker con un token RS256 delegado.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core 10, Oracle 26ai, `HttpClient`, JWT RS256, xUnit, NSubstitute.

## Global Constraints

- Trabajar directamente en `feature/telegram-channel-foundation`; no crear worktree.
- Mantener dependencias `Domain <- Application <- Infrastructure/Api`.
- Aceptar inicialmente solo texto en chats privados.
- No escribir solicitudes ni respuestas en `CHAT_MESSAGES`.
- No registrar tokens, secretos, códigos, texto del usuario, respuesta ni datos personales.
- Derivar `personId` del JWT al emitir códigos; nunca aceptarlo en el cuerpo HTTP.
- Validar el webhook con `X-Telegram-Bot-Api-Secret-Token`.
- Usar `update_id` como idempotencia y `telegram-update-<updateId>` ante el agente.
- Representar IDs de Telegram como `long`/Oracle `NUMBER(19)`.
- Limpiar los textos técnicos del inbox al completar el trabajo.
- Mantener llamadas HTTP fuera de transacciones Oracle.
- No restaurar `Agent__ConversationContextTtlSeconds` ni `Agent__ConversationContextCapacity`: pertenecían al proveedor transitorio eliminado.
- Ejecutar únicamente pruebas focalizadas por tarea y una verificación consolidada al final.

---

### Task 1: Modelar el dominio técnico de Telegram

**Files:**
- Create: `src/Domain/Telegram/Entities/TelegramLinkCode.cs`
- Create: `src/Domain/Telegram/Entities/TelegramUserLink.cs`
- Create: `src/Domain/Telegram/Entities/TelegramConversationLink.cs`
- Create: `src/Domain/Telegram/Entities/TelegramInboundUpdate.cs`
- Create: `src/Domain/Telegram/Enums/TelegramInboundUpdateStatus.cs`
- Test: `tests/Application.Tests/Telegram/Domain/TelegramEntitiesTests.cs`

**Interfaces:**
- Consumes: `Domain.Common.BaseEntity<Guid>` y `Domain.ChatConversations.Entities.ChatConversation` solo mediante ID.
- Produces: fábricas `Create`, operaciones `Consume`, `Relink`, `BindConversation`, `Claim`, `ScheduleRetry`, `PrepareResponse`, `ConfirmChunk` y `Complete`.

- [ ] **Step 1: Escribir pruebas de invariantes**

Cubrir con xUnit:

```csharp
[Fact]
public void Link_code_can_only_be_consumed_once()
{
    var code = TelegramLinkCode.Create(PersonId, "sha256", Now.AddMinutes(10), Now);
    code.Consume(Now.AddMinutes(1));
    Assert.Throws<InvalidOperationException>(() => code.Consume(Now.AddMinutes(2)));
}

[Fact]
public void Completed_update_clears_transient_texts()
{
    var update = TelegramInboundUpdate.Create(42, 1001, 1001, 7, "hola", Now);
    update.PrepareResponse("respuesta", Now);
    update.Complete(Now);
    Assert.Null(update.MessageText);
    Assert.Null(update.ResponseText);
}
```

Añadir pruebas de expiración, IDs positivos/no vacíos, transición de estados, progreso de fragmentos y cambio de conversación.

- [ ] **Step 2: Ejecutar RED**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramEntitiesTests"`

Expected: FAIL porque los tipos `Domain.Telegram` todavía no existen.

- [ ] **Step 3: Implementar entidades mínimas**

Usar constructores privados y fábricas. Definir el estado:

```csharp
public enum TelegramInboundUpdateStatus
{
    Pending,
    Processing,
    Prepared,
    Completed,
    Failed
}
```

`Complete` debe borrar `MessageText`, `ResponseText` y `LastError`; `ScheduleRetry` debe incrementar intentos, guardar únicamente un código seguro de error y volver a `Pending` o pasar a `Failed` al alcanzar el máximo.

- [ ] **Step 4: Ejecutar GREEN y Domain build**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramEntitiesTests"
dotnet build src/Domain/Domain.csproj --no-restore
```

Expected: pruebas aprobadas y compilación sin errores.

- [ ] **Step 5: Commit**

```powershell
git add src/Domain/Telegram tests/Application.Tests/Telegram/Domain
git commit -m "feat: ✨ model Telegram channel state"
```

---

### Task 2: Definir puertos y casos de uso de vinculación e ingreso

**Files:**
- Create: `src/Application/Telegram/Abstractions/ITelegramLinkCodeRepository.cs`
- Create: `src/Application/Telegram/Abstractions/ITelegramUserLinkRepository.cs`
- Create: `src/Application/Telegram/Abstractions/ITelegramConversationLinkRepository.cs`
- Create: `src/Application/Telegram/Abstractions/ITelegramInboundUpdateRepository.cs`
- Create: `src/Application/Telegram/Abstractions/ITelegramLinkCodeProtector.cs`
- Create: `src/Application/Telegram/Abstractions/ITelegramRuntimeSettings.cs`
- Create: `src/Application/Telegram/Linking/CreateTelegramLinkCode.cs`
- Create: `src/Application/Telegram/Linking/ConsumeTelegramLinkCode.cs`
- Create: `src/Application/Telegram/Updates/IngestTelegramUpdate.cs`
- Create: `src/Application/Telegram/Errors/TelegramExceptions.cs`
- Create: `src/Application/Telegram/Abstractions/ITelegramUnitOfWork.cs`
- Test: `tests/Application.Tests/Telegram/Linking/CreateTelegramLinkCodeHandlerTests.cs`
- Test: `tests/Application.Tests/Telegram/Linking/ConsumeTelegramLinkCodeHandlerTests.cs`
- Test: `tests/Application.Tests/Telegram/Updates/IngestTelegramUpdateHandlerTests.cs`

**Interfaces:**
- Consumes: entidades de Task 1, `IUnitOfWork`, `TimeProvider`.
- Produces:

```csharp
public sealed record CreateTelegramLinkCodeCommand(Guid PersonId)
    : IRequest<TelegramLinkCodeResult>;

public sealed record TelegramLinkCodeResult(
    string Code, string DeepLink, DateTimeOffset ExpiresAt);

public sealed record ConsumeTelegramLinkCodeCommand(
    string Code, long TelegramUserId, long TelegramChatId)
    : IRequest<Guid>;

public sealed record IngestTelegramUpdateCommand(
    long UpdateId, long TelegramUserId, long TelegramChatId,
    long TelegramMessageId, string ChatType, string? Text)
    : IRequest<IngestTelegramUpdateResult>;
```

- [ ] **Step 1: Escribir pruebas RED de los tres handlers**

Probar que crear código valida usuario activo, invalida códigos pendientes, guarda solo hash y devuelve deep link; consumir valida vigencia/uso y crea o actualiza el vínculo; ingresar devuelve `Accepted` o `Duplicate` sin ejecutar el agente.

- [ ] **Step 2: Ejecutar RED**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~Application.Tests.Telegram"`

Expected: FAIL por puertos y handlers ausentes.

- [x] **Step 3: Añadir repositorios a un Unit of Work del módulo**

Se implementó `ITelegramUnitOfWork` para evitar acoplar todos los casos de uso
existentes a repositorios que solo pertenecen a Telegram. Expone:

```csharp
ITelegramLinkCodeRepository LinkCodesRepository { get; }
ITelegramUserLinkRepository UserLinksRepository { get; }
ITelegramConversationLinkRepository ConversationLinksRepository { get; }
ITelegramInboundUpdateRepository InboundUpdatesRepository { get; }
```

Definir métodos focalizados: obtener código activo por hash, invalidar pendientes por persona, obtener vínculo por persona/usuario/chat, obtener actualización por ID, agregar, actualizar y reclamar.

- [ ] **Step 4: Implementar handlers y validadores**

El protector debe producir `(RawCode, Hash)`; Application solo persiste `Hash`. El handler de ingreso acepta el contrato mínimo y persiste tipos no soportados para que el worker pueda contestar de forma controlada.

- [ ] **Step 5: Ejecutar GREEN**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~Application.Tests.Telegram"`

Expected: pruebas de Task 1 y Task 2 aprobadas.

- [ ] **Step 6: Commit**

```powershell
git add src/Application/Telegram tests/Application.Tests/Telegram
git commit -m "feat: ✨ add Telegram linking use cases"
```

---

### Task 3: Extraer el dispatcher neutral del agente

**Files:**
- Create: `src/Application/Agent/Abstractions/IAgentMessageDispatcher.cs`
- Create: `src/Application/Agent/Messages/AgentMessageDispatchRequest.cs`
- Create: `src/Application/Agent/Messages/AgentMessageDispatcher.cs`
- Modify: `src/Application/Agent/Abstractions/IConversationContextProvider.cs`
- Modify: `src/Application/Agent/Conversations/PersistentConversationContextProvider.cs`
- Modify: `src/Application/Agent/Conversations/DisabledConversationContextProvider.cs`
- Modify: `src/Application/Agent/Messages/SendAgentMessageHandler.cs`
- Test: `tests/Application.Tests/Agent/Messages/AgentMessageDispatcherTests.cs`
- Test: `tests/Application.Tests/Agent/Messages/SendAgentMessageHandlerTests.cs`
- Test: `tests/Application.Tests/Agent/Conversations/PersistentConversationContextProviderTests.cs`

**Interfaces:**
- Consumes: `AgentConversationContext`, `IAgentMessagingClient` y token opaco.
- Produces:

```csharp
public interface IAgentMessageDispatcher
{
    Task<AgentMessageResult> DispatchAsync(
        AgentMessageDispatchRequest request,
        AgentConversationContext context,
        string accessToken,
        CancellationToken cancellationToken);
}
```

`IConversationContextProvider.ResolveAsync` recibirá `string channel` antes de `idempotencyKey`; solo permitirá valores internos no vacíos y devolverá ese canal.

- [ ] **Step 1: Escribir prueba RED del dispatcher**

Verificar que construya el envelope con `channel = "telegram"`, conserve IDs, rol, escalamiento e idempotencia, y nunca exponga el token en el resultado.

- [ ] **Step 2: Ejecutar RED**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~AgentMessageDispatcherTests"`

Expected: FAIL porque no existe el dispatcher.

- [ ] **Step 3: Implementar dispatcher y adaptar Web/JWT**

El handler existente debe resolver contexto con `channel = "web"`, obtener el token mediante `IUserAccessTokenProvider` y delegar en `IAgentMessageDispatcher`. No cambiar el contrato público de `/api/agent/messages`.

- [ ] **Step 4: Adaptar proveedores y pruebas existentes**

Actualizar firmas y probar que `PersistentConversationContextProvider` devuelve `web` o `telegram` según la entrada, sin modificar su persistencia.

- [ ] **Step 5: Ejecutar GREEN focalizado**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~Agent"`

Expected: todas las pruebas Application del agente aprobadas.

- [ ] **Step 6: Commit**

```powershell
git add src/Application/Agent tests/Application.Tests/Agent
git commit -m "refactor: ♻️ share agent message dispatching"
```

---

### Task 4: Persistir Telegram en Oracle con transacciones reales

**Files:**
- Create: `src/Infrastructure/Telegram/Configuration/TelegramLinkCodeConfiguration.cs`
- Create: `src/Infrastructure/Telegram/Configuration/TelegramUserLinkConfiguration.cs`
- Create: `src/Infrastructure/Telegram/Configuration/TelegramConversationLinkConfiguration.cs`
- Create: `src/Infrastructure/Telegram/Configuration/TelegramInboundUpdateConfiguration.cs`
- Create: `src/Infrastructure/Telegram/Repositories/TelegramLinkCodeRepository.cs`
- Create: `src/Infrastructure/Telegram/Repositories/TelegramUserLinkRepository.cs`
- Create: `src/Infrastructure/Telegram/Repositories/TelegramConversationLinkRepository.cs`
- Create: `src/Infrastructure/Telegram/Repositories/TelegramInboundUpdateRepository.cs`
- Modify: `src/Infrastructure/Persistence/VeterinaryDbContext.cs`
- Create: `src/Infrastructure/Telegram/TelegramUnitOfWork.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Generate: migration pair named `TelegramChannelFoundation` in `src/Infrastructure/Migrations/`
- Modify: `src/Infrastructure/Migrations/VeterinaryDbContextModelSnapshot.cs`
- Modify: `tests/Infrastructure.Tests/Infrastructure.Tests.csproj`
- Test: `tests/Infrastructure.Tests/Telegram/TelegramPersistenceTests.cs`

**Interfaces:**
- Consumes: repositorios de Task 2.
- Produces: tablas Oracle `TELEGRAM_LINK_CODES`, `TELEGRAM_USER_LINKS`, `TELEGRAM_CONVERSATION_LINKS`, `TELEGRAM_INBOUND_UPDATES` y una transacción real en `ExecuteInTransactionAsync`.

- [ ] **Step 1: Escribir pruebas RED de mappings y repositorios**

Validar `VARCHAR2(36)` para GUID, `NUMBER(19)` para IDs externos, `CLOB` para textos técnicos, índices únicos, FKs restrictivas y recuperación de trabajos pendientes.

- [x] **Step 2: Implementar el límite transaccional del módulo**

`TelegramUnitOfWork` usa `Database.BeginTransactionAsync`, confirma en éxito y
revierte ante excepciones. No se conservó SQLite en pruebas porque su dependencia
nativa restaurada presentó una vulnerabilidad de severidad alta; la transacción
relacional debe comprobarse contra la Oracle local antes de la prueba manual.

- [ ] **Step 3: Ejecutar RED focalizado**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramPersistenceTests"`

Expected: FAIL por mappings/repositorios ausentes y falta de rollback real.

- [ ] **Step 4: Implementar configuraciones, repositorios y DI**

Aplicar conversiones Guid-string iguales a `ChatConversationConfiguration`. El reclamo de inbox debe usar una transición condicionada `Pending -> Processing`; no cargar todos los trabajos y filtrarlos en memoria.

- [ ] **Step 5: Implementar transacción real**

Usar `Database.BeginTransactionAsync`, ejecutar la acción, guardar, confirmar y revertir ante excepción. No abrir transacción si el contexto ya tiene una activa; en ese caso participar en ella.

- [ ] **Step 6: Generar y revisar migración**

Run:

```powershell
dotnet ef migrations add TelegramChannelFoundation --project src/Infrastructure/Infrastructure.csproj --startup-project src/Api/Api.csproj
dotnet ef migrations script --project src/Infrastructure/Infrastructure.csproj --startup-project src/Api/Api.csproj --no-build
```

Expected: solo cuatro tablas Telegram, índices y FKs aprobadas; ningún cambio destructivo en tablas existentes.

- [ ] **Step 7: Ejecutar GREEN**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramPersistenceTests"`

Expected: pruebas aprobadas.

- [ ] **Step 8: Commit**

```powershell
git add src/Infrastructure/Telegram src/Infrastructure/Persistence src/Infrastructure/Migrations tests/Infrastructure.Tests/Telegram
git commit -m "feat: ✨ persist Telegram channel state"
```

---

### Task 5: Configurar Telegram, proteger códigos y emitir identidad delegada

**Files:**
- Create: `src/Infrastructure/Telegram/Configuration/TelegramOptions.cs`
- Create: `src/Infrastructure/Telegram/Configuration/TelegramOptionsValidator.cs`
- Create: `src/Infrastructure/Telegram/Configuration/ConfiguredTelegramRuntimeSettings.cs`
- Create: `src/Infrastructure/Telegram/Security/TelegramLinkCodeProtector.cs`
- Create: `src/Application/Telegram/Abstractions/IAgentDelegatedIdentityProvider.cs`
- Create: `src/Application/Telegram/Models/AgentDelegatedIdentity.cs`
- Create: `src/Infrastructure/Telegram/Security/AgentDelegatedIdentityProvider.cs`
- Modify: `src/Infrastructure/Security/Tokens/JwtTokenIssuer.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Test: `tests/Infrastructure.Tests/Telegram/TelegramOptionsValidatorTests.cs`
- Test: `tests/Infrastructure.Tests/Telegram/TelegramLinkCodeProtectorTests.cs`
- Test: `tests/Infrastructure.Tests/Telegram/AgentDelegatedIdentityProviderTests.cs`

**Interfaces:**
- Consumes: `JwtTokenIssuer`, Users, UserAccounts y Roles.
- Produces:

```csharp
public sealed record AgentDelegatedIdentity(
    Guid PersonId, string Role, string AccessToken);

public interface IAgentDelegatedIdentityProvider
{
    Task<AgentDelegatedIdentity> GetAsync(
        Guid personId, CancellationToken cancellationToken);
}
```

- [ ] **Step 1: Escribir pruebas RED de opciones**

Cuando `Telegram:Enabled=false`, permitir valores vacíos. Cuando sea `true`, exigir token, username, secreto de 1-256 caracteres válidos, URL HTTPS absoluta, TTL positivo, intervalo positivo, intentos entre 1 y 10, y token delegado entre 1 y 15 minutos.

- [ ] **Step 2: Escribir pruebas RED de seguridad**

Probar código aleatorio Base64Url, hash SHA-256 determinista sin conservar el valor y JWT delegado con `person_id`, `role`, issuer, audience y expiración configurada. Rechazar usuario/cuenta inactivos.

- [ ] **Step 3: Ejecutar RED**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramOptionsValidatorTests|FullyQualifiedName~TelegramLinkCodeProtectorTests|FullyQualifiedName~AgentDelegatedIdentityProviderTests"`

Expected: FAIL por implementaciones ausentes.

- [ ] **Step 4: Implementar y registrar**

Añadir a `JwtTokenIssuer` una sobrecarga que reciba duración sin emitir refresh token. El proveedor debe construir `AuthenticatedIdentity` desde repositorios, nunca desde datos de Telegram.

- [ ] **Step 5: Ejecutar GREEN**

Run: mismo comando del Step 3.

Expected: pruebas aprobadas.

- [ ] **Step 6: Commit**

```powershell
git add src/Application/Telegram src/Infrastructure/Telegram src/Infrastructure/Security/Tokens/JwtTokenIssuer.cs src/Infrastructure/DependencyInjection.cs tests/Infrastructure.Tests/Telegram
git commit -m "feat: ✨ secure Telegram channel configuration"
```

---

### Task 6: Implementar cliente saliente y fragmentación de respuestas

**Files:**
- Create: `src/Application/Telegram/Abstractions/ITelegramBotClient.cs`
- Create: `src/Application/Telegram/Messages/TelegramTextChunker.cs`
- Create: `src/Infrastructure/Telegram/Http/TelegramBotHttpClient.cs`
- Create: `src/Infrastructure/Telegram/Http/Contracts/TelegramSendMessageRequest.cs`
- Create: `src/Infrastructure/Telegram/Http/Contracts/TelegramSendMessageResponse.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Test: `tests/Application.Tests/Telegram/Messages/TelegramTextChunkerTests.cs`
- Test: `tests/Infrastructure.Tests/Telegram/TelegramBotHttpClientTests.cs`

**Interfaces:**
- Produces `Task<long> SendTextAsync(long chatId, string text, CancellationToken)` y `IReadOnlyList<string> Split(string text, int maximumLength = 4096)`.

- [ ] **Step 1: Escribir pruebas RED**

Probar texto corto, exactamente 4096 caracteres, texto largo con párrafos y Unicode, URL/token correctos, JSON de `sendMessage`, respuesta exitosa y errores 429/5xx traducidos a excepciones temporales.

- [ ] **Step 2: Ejecutar RED**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramTextChunkerTests"; dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramBotHttpClientTests"`

Expected: FAIL por tipos ausentes.

- [ ] **Step 3: Implementar cliente y chunker**

Enviar texto plano sin `parse_mode`. No incluir el token en mensajes de excepción ni logs. Respetar cancelación y `Retry-After` cuando Telegram lo entregue.

- [ ] **Step 4: Ejecutar GREEN**

Run: mismo comando del Step 2.

Expected: pruebas aprobadas.

- [ ] **Step 5: Commit**

```powershell
git add src/Application/Telegram src/Infrastructure/Telegram tests/Application.Tests/Telegram tests/Infrastructure.Tests/Telegram
git commit -m "feat: ✨ send Telegram text responses"
```

---

### Task 7: Exponer códigos de vinculación y webhook

**Files:**
- Create: `src/Api/Telegram/Controllers/TelegramLinkCodesController.cs`
- Create: `src/Api/Telegram/Controllers/TelegramWebhookController.cs`
- Create: `src/Api/Telegram/Dtos/CreateTelegramLinkCodeResponse.cs`
- Create: `src/Api/Telegram/Dtos/TelegramUpdateRequest.cs`
- Create: `src/Api/Telegram/Security/TelegramWebhookSecretValidator.cs`
- Create: `src/Api/Telegram/DependencyInjection.cs`
- Modify: `src/Api/Program.cs`
- Modify: `src/Api/Common/Errors/GlobalExceptionHandler.cs`
- Test: `tests/Api.Tests/Telegram/TelegramLinkCodesHttpTests.cs`
- Test: `tests/Api.Tests/Telegram/TelegramWebhookHttpTests.cs`

**Interfaces:**
- Endpoint autenticado `POST /api/integrations/telegram/link-codes` devuelve `201 Created`.
- Endpoint propio `POST /api/integrations/telegram/webhook` devuelve `200 OK` para aceptado, duplicado o contenido reconocido como no soportado.

- [ ] **Step 1: Escribir pruebas RED HTTP**

Probar 401 sin JWT en link-codes, derivación de `person_id`, respuesta sin hash, 401 ante secreto ausente/incorrecto, comparación válida, rechazo de JSON malformado y 200 ante update duplicado.

- [ ] **Step 2: Ejecutar RED**

Run: `dotnet test tests/Api.Tests/Api.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramLinkCodesHttpTests|FullyQualifiedName~TelegramWebhookHttpTests"`

Expected: FAIL con rutas 404.

- [ ] **Step 3: Implementar DTOs y controladores**

Mapear únicamente `update_id`, `message.message_id`, `message.from.id`, `message.chat.id`, `message.chat.type` y `message.text`. No persistir el objeto JSON completo.

- [ ] **Step 4: Añadir rate limiting y OpenAPI**

Registrar una política `telegram-webhook` independiente y documentar el header secreto sin valor de ejemplo. Mantener autorización `authenticated-fallback` para códigos y autenticación propia para webhook.

- [ ] **Step 5: Ejecutar GREEN**

Run: mismo comando del Step 2.

Expected: pruebas aprobadas.

- [ ] **Step 6: Commit**

```powershell
git add src/Api/Telegram src/Api/Program.cs src/Api/Common/Errors/GlobalExceptionHandler.cs tests/Api.Tests/Telegram
git commit -m "feat: ✨ expose Telegram integration endpoints"
```

---

### Task 8: Procesar vinculación y conversaciones desde el worker

**Files:**
- Create: `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs`
- Create: `src/Application/Telegram/Processing/TelegramConversationResolver.cs`
- Create: `src/Application/Telegram/Processing/TelegramControlMessages.cs`
- Test: `tests/Application.Tests/Telegram/Processing/ProcessTelegramLinkUpdateTests.cs`
- Test: `tests/Application.Tests/Telegram/Processing/TelegramConversationResolverTests.cs`

**Interfaces:**
- Consumes: casos de Task 2, `IConversationContextProvider`, vínculos Telegram y `ITelegramBotClient`.
- Produces: procesamiento de `/start`, usuario no vinculado, contenido no soportado y resolución atómica de conversación.

- [ ] **Step 1: Escribir pruebas RED de control**

Probar `/start` válido/inválido/vencido, usuario sin vínculo, chat no privado y texto ausente. Ningún caso de control debe invocar `IAgentMessagingClient` ni crear `CHAT_MESSAGES`.

- [ ] **Step 2: Escribir pruebas RED de conversación**

Probar creación inicial, reutilización abierta, reutilización escalada y creación nueva al encontrar `Closed=true`. Verificar una sola conversación bajo dos solicitudes secuenciales del mismo chat.

- [ ] **Step 3: Ejecutar RED**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~ProcessTelegramLinkUpdateTests|FullyQualifiedName~TelegramConversationResolverTests"`

Expected: FAIL por procesador y resolver ausentes.

- [ ] **Step 4: Implementar control y resolución**

Ejecutar creación de contexto y actualización del vínculo dentro de `IUnitOfWork.ExecuteInTransactionAsync`. Salir de la transacción antes de cualquier `SendTextAsync`.

- [ ] **Step 5: Ejecutar GREEN**

Run: mismo comando del Step 3.

Expected: pruebas aprobadas.

- [ ] **Step 6: Commit**

```powershell
git add src/Application/Telegram tests/Application.Tests/Telegram
git commit -m "feat: ✨ resolve Telegram users and conversations"
```

---

### Task 9: Completar procesamiento agente, reintentos y hosted service

**Files:**
- Modify: `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs`
- Create: `src/Infrastructure/Telegram/Workers/TelegramUpdateWorker.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Test: `tests/Application.Tests/Telegram/Processing/ProcessTelegramMessageUpdateTests.cs`
- Test: `tests/Infrastructure.Tests/Telegram/TelegramUpdateWorkerTests.cs`

**Interfaces:**
- Consumes: `IAgentMessageDispatcher`, `IAgentDelegatedIdentityProvider`, contexto resuelto, chunker, bot client e inbox.
- Produces: flujo completo y recuperación de pendientes después de reiniciar.

- [ ] **Step 1: Escribir pruebas RED del flujo feliz**

Verificar identidad delegada, `channel = "telegram"`, idempotency key determinista, respuesta dividida, envío ordenado, progreso por fragmento, limpieza final y ausencia de llamadas a `ChatMessagesRepository`.

- [ ] **Step 2: Escribir pruebas RED de reintento**

Probar error temporal del agente, error temporal de Telegram, máximo de intentos, recuperación de un trabajo pendiente y que dos workers no reclamen el mismo update.

- [ ] **Step 3: Ejecutar RED**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~ProcessTelegramMessageUpdateTests"; dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramUpdateWorkerTests"`

Expected: FAIL por flujo y worker incompletos.

- [ ] **Step 4: Implementar procesamiento externo**

Persistir la respuesta preparada antes del primer `sendMessage`. Tras cada envío guardar `LastSentChunkIndex`. En éxito llamar `Complete`; en error usar espera incremental `1s, 2s, 4s` limitada por `MaxProcessingAttempts`.

- [ ] **Step 5: Implementar hosted service**

Registrar `AddHostedService<TelegramUpdateWorker>()` solo cuando Telegram esté habilitado. Crear un scope por iteración, respetar `CancellationToken` y no usar `Task.Delay` sin cancelación.

- [ ] **Step 6: Ejecutar GREEN**

Run: mismo comando del Step 3.

Expected: pruebas aprobadas.

- [ ] **Step 7: Commit**

```powershell
git add src/Application/Telegram src/Infrastructure/Telegram src/Infrastructure/DependencyInjection.cs tests/Application.Tests/Telegram tests/Infrastructure.Tests/Telegram
git commit -m "feat: ✨ process Telegram updates asynchronously"
```

---

### Task 10: Documentar configuración, túnel y operación del webhook

**Files:**
- Modify: `.env.example`
- Modify: `README.md`
- Create: `docs/integrations/telegram.md`
- Test: `tests/Api.Tests/Telegram/TelegramOptionsStartupTests.cs`

**Interfaces:**
- Consumes: nombres exactos de configuración de Task 5.
- Produces: instrucciones reproducibles para Swagger, túnel VS Code, `setWebhook`, `getWebhookInfo`, vinculación y prueba real.

- [ ] **Step 1: Escribir prueba RED de startup**

Probar que `Telegram__Enabled=false` no exige secretos y que `true` falla con un mensaje seguro si falta cada valor requerido.

- [ ] **Step 2: Ejecutar RED**

Run: `dotnet test tests/Api.Tests/Api.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramOptionsStartupTests"`

Expected: FAIL hasta completar configuración/DI.

- [ ] **Step 3: Actualizar `.env.example` y README**

Añadir todas las variables `Telegram__*` vacías o con defaults seguros. Documentar explícitamente:

```text
Agent__ConversationContextTtlSeconds y Agent__ConversationContextCapacity
fueron retiradas porque solo configuraban el proveedor transitorio en memoria.
El contexto actual usa Oracle y los catálogos Agent__InitialConversationStatusId
y Agent__ClientParticipantTypeId.
```

- [ ] **Step 4: Escribir guía operativa**

Incluir comandos PowerShell que lean el token desde una variable sin imprimirlo, registren `${PublicUrl}/api/integrations/telegram/webhook` con `secret_token`, consulten `getWebhookInfo`, generen el código desde Swagger y prueben `/start` seguido de texto.

- [ ] **Step 5: Ejecutar GREEN y comprobar secretos**

Run:

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --no-restore --filter "FullyQualifiedName~TelegramOptionsStartupTests"
rg -n "BotToken=.+|WebhookSecret=.+" .env.example README.md docs/integrations/telegram.md
```

Expected: pruebas aprobadas y `rg` sin secretos no vacíos.

- [ ] **Step 6: Commit**

```powershell
git add .env.example README.md docs/integrations/telegram.md tests/Api.Tests/Telegram
git commit -m "docs: 📝 document Telegram channel setup"
```

---

### Task 11: Verificación consolidada y auditoría

**Files:**
- Review: todos los archivos modificados en Tasks 1-10.

**Interfaces:**
- Produces: evidencia de compatibilidad, migración limpia y recorrido funcional preparado para prueba manual.

- [ ] **Step 1: Ejecutar pruebas focalizadas consolidadas**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~Agent|FullyQualifiedName~Telegram"
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --no-restore --filter "FullyQualifiedName~Agent|FullyQualifiedName~Telegram"
dotnet test tests/Api.Tests/Api.Tests.csproj --no-restore --filter "FullyQualifiedName~Agent|FullyQualifiedName~Telegram"
```

Expected: cero fallos. No ejecutar la suite completa salvo autorización posterior.

- [ ] **Step 2: Compilar y comprobar migración**

Run:

```powershell
dotnet build veterinarian_backend.slnx --no-restore
dotnet ef migrations has-pending-model-changes --project src/Infrastructure/Infrastructure.csproj --startup-project src/Api/Api.csproj --no-build
```

Expected: 0 errores, 0 advertencias y sin cambios pendientes del modelo.

- [ ] **Step 3: Auditar límites y seguridad**

Run:

```powershell
rg -n "ChatMessagesRepository|CHAT_MESSAGES" src/Application/Telegram src/Infrastructure/Telegram src/Api/Telegram
rg -n "BotToken|WebhookSecret|Authorization|MessageText|ResponseText" src/Infrastructure/Telegram src/Api/Telegram
git diff --check develop...HEAD
git status --short
```

Expected: ninguna escritura de mensajes; ningún log con valores sensibles; diff válido y árbol limpio.

- [ ] **Step 4: Prueba manual controlada**

Aplicar la migración únicamente contra la Oracle local confirmada, iniciar backend y chatbot, abrir túnel HTTPS, registrar webhook, generar código con JWT desde Swagger, consumir `/start`, enviar texto y comprobar conversación/participante/vínculos en Oracle. No aplicar migraciones a producción.

- [ ] **Step 5: Revisión y commit de correcciones verificadas**

Si la verificación exige cambios, aplicar únicamente correcciones relacionadas, repetir el comando afectado y usar:

```powershell
git add src/Domain/Telegram src/Application/Telegram src/Application/Agent src/Infrastructure/Telegram src/Api/Telegram tests/Application.Tests/Telegram tests/Infrastructure.Tests/Telegram tests/Api.Tests/Telegram
git commit -m "fix: 🐛 stabilize Telegram channel flow"
```

Si no hay cambios, no crear un commit vacío.
