# Agent Messaging Gateway Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Exponer `POST /api/agent/messages` desde .NET para reenviar de forma autenticada, configurable e idempotente mensajes manuales de Swagger hacia `POST /api/v1/messages` de Huellitas ChatBot.

**Architecture:** `Api.Agent` deriva identidad y headers confiables; `Application.Agent` coordina un contexto conversacional neutral y el puerto de mensajería; `Infrastructure.Agent` resuelve temporalmente conversaciones en memoria y encapsula el HTTP de FastAPI. Ninguna pieza de `Agent` persiste en Oracle ni conoce tablas de conversaciones, participantes, mensajes, ejecuciones o escalamiento.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR 14, FluentValidation 12, `IHttpClientFactory`, `System.Text.Json`, xUnit 2.9, FastAPI HTTP/JSON y JWT RS256 existente.

## Global Constraints

- Trabajar en `feature/agent-messaging-gateway` sobre el repositorio normal; no crear worktree.
- Mantener `conversationId` como UUID interno; nunca usar IDs de Telegram o de otros canales como identidad canónica.
- No crear migraciones, repositorios ni entidades Oracle para conversaciones, participantes, mensajes, ejecuciones o escalamiento.
- No agregar código a `Domain/Agent` mientras no existan invariantes persistentes.
- El contrato público no acepta `userId`, `roles`, `channel`, `isEscalated` ni `publishAsGlobalKnowledge`.
- Derivar `person_id` y `role` exclusivamente del JWT validado por .NET.
- Reenviar el access token únicamente en `Authorization: Bearer`; nunca registrarlo, persistirlo ni incluirlo en respuestas.
- Configurar ubicación, ruta, timeout y límites mediante variables `Agent__*`; no codificar la URL del chatbot.
- No realizar reintentos HTTP automáticos, no incorporar Redis ni circuit breaker.
- Las pruebas automáticas no llaman a Oracle, FastAPI, Qdrant ni proveedores reales.
- Mantener todos los proyectos de prueba bajo `tests/`.
- Usar Conventional Commits con `:sparkles:`, `:bug:` o `:memo:` según corresponda.

---

## File map

### Application

- Create `src/Application/Agent/Abstractions/IAgentMessagingClient.cs`: puerto HTTP neutral.
- Create `src/Application/Agent/Abstractions/IConversationContextProvider.cs`: resolución sustituible de conversación.
- Create `src/Application/Agent/Abstractions/IUserAccessTokenProvider.cs`: acceso seguro al Bearer actual.
- Create `src/Application/Agent/Errors/AgentGatewayExceptions.cs`: categorías neutrales consumidas por API.
- Create `src/Application/Agent/Messages/AgentConversationContext.cs`: contexto resuelto.
- Create `src/Application/Agent/Messages/AgentMessageEnvelope.cs`: solicitud neutral interna.
- Create `src/Application/Agent/Messages/AgentMessageResult.cs`: respuesta neutral.
- Create `src/Application/Agent/Messages/SendAgentMessageCommand.cs`: comando MediatR.
- Create `src/Application/Agent/Messages/SendAgentMessageCommandValidator.cs`: reglas del contrato.
- Create `src/Application/Agent/Messages/SendAgentMessageHandler.cs`: coordinación sin HTTP ni Oracle.
- Create `tests/Application.Tests/Agent/Messages/SendAgentMessageHandlerTests.cs`.
- Create `tests/Application.Tests/Agent/Messages/SendAgentMessageCommandValidatorTests.cs`.

### Infrastructure

- Create `tests/Infrastructure.Tests/Infrastructure.Tests.csproj` and add it to `veterinarian_backend.slnx`.
- Create `src/Infrastructure/Agent/Configuration/AgentOptions.cs`.
- Create `src/Infrastructure/Agent/Configuration/AgentOptionsValidator.cs`.
- Create `src/Infrastructure/Agent/Conversations/TransientConversationContextProvider.cs`.
- Create `src/Infrastructure/Agent/Http/AgentMessagingHttpClient.cs`.
- Create `src/Infrastructure/Agent/Http/DisabledAgentMessagingClient.cs`.
- Create `src/Infrastructure/Agent/Http/Contracts/AgentHttpRequest.cs`.
- Create `src/Infrastructure/Agent/Http/Contracts/AgentHttpResponse.cs`.
- Modify `src/Infrastructure/DependencyInjection.cs`: options, transient provider and typed client.
- Create `tests/Infrastructure.Tests/Agent/Configuration/AgentOptionsValidatorTests.cs`.
- Create `tests/Infrastructure.Tests/Agent/Conversations/TransientConversationContextProviderTests.cs`.
- Create `tests/Infrastructure.Tests/Agent/Http/AgentMessagingHttpClientTests.cs`.

### API

- Create `src/Api/Agent/Dtos/SendAgentMessageRequest.cs`.
- Create `src/Api/Agent/Dtos/SendAgentMessageResponse.cs`.
- Create `src/Api/Agent/Security/HttpContextUserAccessTokenProvider.cs`.
- Create `src/Api/Agent/Controllers/AgentMessagesController.cs`.
- Create `src/Api/Agent/DependencyInjection.cs`.
- Modify `src/Api/Program.cs`: register API-side Agent services.
- Modify `src/Api/Common/Errors/GlobalExceptionHandler.cs`: safe Agent mappings.
- Create `tests/Api.Tests/Agent/AgentMessagesHttpTests.cs`.

### Configuration and docs

- Modify `.env.example`: safe `Agent__*` entries; blank any real JWT key material if still present.
- Modify `README.md`: local Swagger flow and environment variables.
- Create `tests/Api.Tests/Agent/AgentOptionsStartupTests.cs`: startup validation without Oracle access.
- Modify `docs/plans/2026-08-28-agent-messaging-gateway-design.md` only if implementation review finds a factual mismatch.

---

### Task 1: Neutral Application use case

**Files:**
- Create: `src/Application/Agent/Abstractions/IAgentMessagingClient.cs`
- Create: `src/Application/Agent/Abstractions/IConversationContextProvider.cs`
- Create: `src/Application/Agent/Abstractions/IUserAccessTokenProvider.cs`
- Create: `src/Application/Agent/Errors/AgentGatewayExceptions.cs`
- Create: `src/Application/Agent/Messages/AgentConversationContext.cs`
- Create: `src/Application/Agent/Messages/AgentMessageEnvelope.cs`
- Create: `src/Application/Agent/Messages/AgentMessageResult.cs`
- Create: `src/Application/Agent/Messages/SendAgentMessageCommand.cs`
- Create: `src/Application/Agent/Messages/SendAgentMessageCommandValidator.cs`
- Create: `src/Application/Agent/Messages/SendAgentMessageHandler.cs`
- Test: `tests/Application.Tests/Agent/Messages/SendAgentMessageHandlerTests.cs`
- Test: `tests/Application.Tests/Agent/Messages/SendAgentMessageCommandValidatorTests.cs`

**Interfaces:**
- Consumes: MediatR `IRequest<T>`, FluentValidation and `CancellationToken`.
- Produces: `IAgentMessagingClient.SendAsync(AgentMessageEnvelope, string, CancellationToken)`, `IConversationContextProvider.ResolveAsync(Guid, Guid?, string, CancellationToken)`, `IUserAccessTokenProvider.GetRequiredAccessToken()` and `SendAgentMessageCommand`.

- [ ] **Step 1: Write failing handler tests with hand-written fakes**

Use fixed values and verify that identity/control fields cannot come from the caller body:

```csharp
private static readonly Guid PersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
private static readonly Guid ConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
private static readonly Guid CorrelationId = Guid.Parse("33333333-3333-3333-3333-333333333333");

[Fact]
public async Task Handle_builds_envelope_from_authenticated_command_and_resolved_context()
{
    var conversations = new RecordingConversationContextProvider(
        new AgentConversationContext(ConversationId, "web", false));
    var client = new RecordingAgentMessagingClient(new AgentMessageResult(
        "Respuesta", ConversationId, CorrelationId, "ai_generated", null));
    var handler = new SendAgentMessageHandler(
        conversations,
        new StubUserAccessTokenProvider("signed-access-token"),
        client);
    var command = new SendAgentMessageCommand(
        "¿Qué vacunas necesita?",
        null,
        null,
        "es-CO",
        PersonId,
        "Cliente",
        "message-001",
        CorrelationId);

    var result = await handler.Handle(command, CancellationToken.None);

    Assert.Equal(ConversationId, result.ConversationId);
    Assert.Equal(PersonId, client.Envelope!.UserId);
    Assert.Equal(["Cliente"], client.Envelope.Roles);
    Assert.Equal("web", client.Envelope.Channel);
    Assert.False(client.Envelope.IsEscalated);
    Assert.False(client.Envelope.PublishAsGlobalKnowledge);
    Assert.Equal("signed-access-token", client.AccessToken);
}
```

Add focused tests named:

```text
Handle_passes_requested_conversation_to_context_provider
Handle_propagates_cancellation_to_both_ports
Handle_returns_human_controlled_result_without_rewriting_it
```

- [ ] **Step 2: Run handler tests and verify RED**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj `
  --filter FullyQualifiedName~Agent.Messages.SendAgentMessageHandlerTests
```

Expected: compilation fails because `Application.Agent` contracts do not exist.

- [ ] **Step 3: Add exact neutral records and port signatures**

Implement these public shapes:

```csharp
public sealed record AgentConversationContext(
    Guid ConversationId,
    string Channel,
    bool IsEscalated);

public sealed record AgentMessageEnvelope(
    string Message,
    Guid ConversationId,
    Guid UserId,
    Guid? PetId,
    string Channel,
    string Language,
    IReadOnlyList<string> Roles,
    bool IsEscalated,
    Guid CorrelationId,
    string IdempotencyKey,
    bool PublishAsGlobalKnowledge);

public sealed record AgentMessageResult(
    string? Message,
    Guid ConversationId,
    Guid CorrelationId,
    string ResponseType,
    string? Module);

public interface IAgentMessagingClient
{
    Task<AgentMessageResult> SendAsync(
        AgentMessageEnvelope message,
        string accessToken,
        CancellationToken cancellationToken);
}

public interface IConversationContextProvider
{
    ValueTask<AgentConversationContext> ResolveAsync(
        Guid personId,
        Guid? requestedConversationId,
        string idempotencyKey,
        CancellationToken cancellationToken);
}

public interface IUserAccessTokenProvider
{
    string GetRequiredAccessToken();
}
```

Define exceptions in `AgentGatewayExceptions.cs` with safe default messages and optional inner exceptions:

```csharp
public abstract class AgentGatewayException : Exception
{
    protected AgentGatewayException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

public sealed class AgentUnavailableException(Exception? innerException = null)
    : AgentGatewayException("Agent service is unavailable.", innerException) { }
public sealed class AgentTimeoutException(Exception? innerException = null)
    : AgentGatewayException("Agent service timed out.", innerException) { }
public sealed class AgentContractException(Exception? innerException = null)
    : AgentGatewayException("Agent service returned an invalid contract.", innerException) { }
public sealed class AgentAuthenticationException(Exception? innerException = null)
    : AgentGatewayException("Agent service rejected backend authentication.", innerException) { }
public sealed class AgentIdempotencyConflictException()
    : AgentGatewayException("Agent idempotency key conflicts with another request.") { }
public sealed class AgentConversationCapacityException()
    : AgentGatewayException("Transient conversation capacity is exhausted.") { }
```

- [ ] **Step 4: Implement command, validator and handler minimally**

Use this command signature:

```csharp
public sealed record SendAgentMessageCommand(
    string Message,
    Guid? ConversationId,
    Guid? PetId,
    string Language,
    Guid PersonId,
    string Role,
    string IdempotencyKey,
    Guid CorrelationId) : IRequest<AgentMessageResult>;
```

Validator rules:

```csharp
RuleFor(x => x.Message).NotEmpty().MaximumLength(8000);
RuleFor(x => x.Language).NotEmpty().MaximumLength(20);
RuleFor(x => x.PersonId).NotEmpty();
RuleFor(x => x.Role).NotEmpty().MaximumLength(80);
RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(160);
RuleFor(x => x.CorrelationId).NotEmpty();
```

The handler must resolve the context, obtain the token and send exactly one
envelope. It must not catch neutral gateway exceptions.

Use this orchestration body so the handler remains independent of HTTP and
persistence:

```csharp
public async Task<AgentMessageResult> Handle(
    SendAgentMessageCommand request,
    CancellationToken cancellationToken)
{
    var context = await conversationContextProvider.ResolveAsync(
        request.PersonId,
        request.ConversationId,
        request.IdempotencyKey,
        cancellationToken);
    var accessToken = userAccessTokenProvider.GetRequiredAccessToken();
    var envelope = new AgentMessageEnvelope(
        request.Message,
        context.ConversationId,
        request.PersonId,
        request.PetId,
        context.Channel,
        request.Language,
        [request.Role],
        context.IsEscalated,
        request.CorrelationId,
        request.IdempotencyKey,
        false);

    return await agentMessagingClient.SendAsync(
        envelope,
        accessToken,
        cancellationToken);
}
```

- [ ] **Step 5: Write validator tests and verify GREEN**

Cover empty/oversized message, empty/oversized language, empty person, blank
role, blank/oversized idempotency and empty correlation ID. Use
`ValidateAsync` directly and assert the exact property name in each failure.

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj `
  --filter FullyQualifiedName~Application.Tests.Agent
```

Expected: all Agent Application tests pass.

- [ ] **Step 6: Commit Task 1**

```powershell
git add src/Application/Agent tests/Application.Tests/Agent
git commit -m "feat: :sparkles: define agent messaging use case"
```

---

### Task 2: Validated options and transient conversation context

**Files:**
- Create: `tests/Infrastructure.Tests/Infrastructure.Tests.csproj`
- Modify: `veterinarian_backend.slnx`
- Create: `src/Infrastructure/Agent/Configuration/AgentOptions.cs`
- Create: `src/Infrastructure/Agent/Configuration/AgentOptionsValidator.cs`
- Create: `src/Infrastructure/Agent/Conversations/TransientConversationContextProvider.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Test: `tests/Infrastructure.Tests/Agent/Configuration/AgentOptionsValidatorTests.cs`
- Test: `tests/Infrastructure.Tests/Agent/Conversations/TransientConversationContextProviderTests.cs`

**Interfaces:**
- Consumes: `IConversationContextProvider`, `AgentConversationContext`, `AgentConversationCapacityException` and `TimeProvider`.
- Produces: validated `AgentOptions` and singleton `TransientConversationContextProvider`.

- [ ] **Step 1: Add the Infrastructure test project**

Create a `net10.0` xUnit project with these references:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="..\..\src\Infrastructure\Infrastructure.csproj" />
</ItemGroup>
```

Add `<Project Path="tests/Infrastructure.Tests/Infrastructure.Tests.csproj" />`
to `veterinarian_backend.slnx`.

- [ ] **Step 2: Write failing options tests**

Define valid defaults and assert:

```text
Disabled_options_accept_empty_address
Enabled_options_require_absolute_http_or_https_base_url
Enabled_options_require_relative_messages_path_starting_with_slash
Enabled_options_require_timeout_between_1_and_120_seconds
Enabled_options_require_ttl_between_30_and_3600_seconds
Enabled_options_require_capacity_between_1_and_100000
Enabled_options_require_max_response_bytes_between_1024_and_1048576
```

Run:

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj `
  --filter FullyQualifiedName~AgentOptionsValidatorTests
```

Expected: compilation fails because options do not exist.

- [ ] **Step 3: Implement `AgentOptions` and its validator**

Use:

```csharp
public sealed class AgentOptions
{
    public const string SectionName = "Agent";
    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public string MessagesPath { get; init; } = "/api/v1/messages";
    public int RequestTimeoutSeconds { get; init; } = 30;
    public int ConversationContextTtlSeconds { get; init; } = 900;
    public int ConversationContextCapacity { get; init; } = 10_000;
    public int MaxResponseBytes { get; init; } = 1_048_576;
}
```

When enabled, reject a BaseUrl with non-HTTP scheme, credentials, query or
fragment. Reject an absolute `MessagesPath`, `..`, query or fragment.

- [ ] **Step 4: Write failing transient context tests**

Use `FakeTimeProvider` implemented inside the test file and assert:

```csharp
[Fact]
public async Task Resolve_without_id_reuses_generated_id_for_same_person_and_key()
{
    var provider = CreateProvider(capacity: 10, ttlSeconds: 60);

    var first = await provider.ResolveAsync(PersonId, null, "message-001", default);
    var retry = await provider.ResolveAsync(PersonId, null, "message-001", default);

    Assert.NotEqual(Guid.Empty, first.ConversationId);
    Assert.Equal(first.ConversationId, retry.ConversationId);
    Assert.Equal("web", first.Channel);
    Assert.False(first.IsEscalated);
}
```

Also verify different persons/keys get different IDs, a requested ID bypasses
the map, expired entries are replaced, cancellation is honored before locking,
and full unexpired capacity throws `AgentConversationCapacityException`.

- [ ] **Step 5: Implement the bounded provider**

Use a private lock and dictionary keyed by the exact record struct:

```csharp
private readonly record struct ConversationKey(Guid PersonId, string IdempotencyKey);
private sealed record Entry(Guid ConversationId, DateTimeOffset ExpiresAt);
```

Inside `ResolveAsync`: throw on cancellation, trim the key, return a supplied
ID directly, remove expired entries, reuse an existing unexpired entry, enforce
capacity, then add `Guid.NewGuid()` with `TimeProvider.GetUtcNow() + TTL`.

The critical section must follow this exact order:

```csharp
cancellationToken.ThrowIfCancellationRequested();
if (requestedConversationId is { } suppliedId)
{
    return ValueTask.FromResult(new AgentConversationContext(suppliedId, "web", false));
}

var key = new ConversationKey(personId, idempotencyKey.Trim());
lock (sync)
{
    var now = timeProvider.GetUtcNow();
    foreach (var expired in entries.Where(pair => pair.Value.ExpiresAt <= now)
                 .Select(pair => pair.Key).ToArray())
    {
        entries.Remove(expired);
    }

    if (entries.TryGetValue(key, out var existing))
    {
        return ValueTask.FromResult(
            new AgentConversationContext(existing.ConversationId, "web", false));
    }

    if (entries.Count >= capacity)
    {
        throw new AgentConversationCapacityException();
    }

    var conversationId = Guid.NewGuid();
    entries.Add(key, new Entry(conversationId, now.Add(ttl)));
    return ValueTask.FromResult(
        new AgentConversationContext(conversationId, "web", false));
}
```

- [ ] **Step 6: Register options and context provider**

In `AddInfrastructure`:

```csharp
services.AddSingleton<IValidateOptions<AgentOptions>, AgentOptionsValidator>();
services.AddOptions<AgentOptions>()
    .Bind(configuration.GetSection(AgentOptions.SectionName))
    .ValidateOnStart();
services.AddSingleton<IConversationContextProvider, TransientConversationContextProvider>();
```

Run:

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj `
  --filter "FullyQualifiedName~AgentOptionsValidatorTests|FullyQualifiedName~TransientConversationContextProviderTests"
```

Expected: all selected tests pass.

- [ ] **Step 7: Commit Task 2**

```powershell
git add veterinarian_backend.slnx src/Infrastructure/Agent `
  src/Infrastructure/DependencyInjection.cs tests/Infrastructure.Tests
git commit -m "feat: :sparkles: add transient agent conversation context"
```

---

### Task 3: Typed FastAPI HTTP adapter

**Files:**
- Create: `src/Infrastructure/Agent/Http/Contracts/AgentHttpRequest.cs`
- Create: `src/Infrastructure/Agent/Http/Contracts/AgentHttpResponse.cs`
- Create: `src/Infrastructure/Agent/Http/AgentMessagingHttpClient.cs`
- Create: `src/Infrastructure/Agent/Http/DisabledAgentMessagingClient.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Test: `tests/Infrastructure.Tests/Agent/Http/AgentMessagingHttpClientTests.cs`

**Interfaces:**
- Consumes: `IAgentMessagingClient`, `AgentMessageEnvelope`, `AgentMessageResult`, `AgentOptions` and neutral exceptions.
- Produces: one typed HTTP call with Bearer forwarding and safe response mapping.

- [ ] **Step 1: Write failing serialization and success tests**

Create a `RecordingHttpMessageHandler` that captures a cloned request body and
returns configured responses. Verify:

```csharp
[Fact]
public async Task Send_posts_expected_fastapi_contract_and_maps_safe_result()
{
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK, """
        {
          "message":"Respuesta",
          "conversationId":"22222222-2222-2222-2222-222222222222",
          "correlationId":"33333333-3333-3333-3333-333333333333",
          "responseType":"ai_generated",
          "provider":"openai",
          "model":"gpt-4o-mini",
          "usage":{"inputTokens":10,"outputTokens":5},
          "module":null,
          "rag":{"status":"empty"}
        }
        """);
    var client = CreateClient(handler);

    var result = await client.SendAsync(Envelope(), "secret-token", default);

    Assert.Equal(HttpMethod.Post, handler.Request!.Method);
    Assert.Equal("/api/v1/messages", handler.Request.RequestUri!.AbsolutePath);
    Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
    Assert.Equal("secret-token", handler.Request.Headers.Authorization.Parameter);
    Assert.Equal("Respuesta", result.Message);
    Assert.DoesNotContain("provider", JsonSerializer.Serialize(result));
}
```

Parse the captured request JSON and assert all 12 FastAPI fields, including
`publishAsGlobalKnowledge=false`.

- [ ] **Step 2: Write failing error tests**

Cover exact translations:

```text
401 and 403 -> AgentAuthenticationException
409 -> AgentIdempotencyConflictException
400 and 422 -> AgentContractException
429, 502 and 503 -> AgentUnavailableException
504 -> AgentTimeoutException
invalid success JSON -> AgentContractException
mismatched conversationId or correlationId -> AgentContractException
response above MaxResponseBytes -> AgentContractException
HttpRequestException -> AgentUnavailableException
timeout cancellation not requested by caller -> AgentTimeoutException
caller cancellation -> original OperationCanceledException
```

- [ ] **Step 3: Run HTTP tests and verify RED**

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj `
  --filter FullyQualifiedName~AgentMessagingHttpClientTests
```

Expected: compilation fails because the HTTP adapter does not exist.

- [ ] **Step 4: Implement private HTTP contracts**

Use records with explicit `JsonPropertyName` attributes for the FastAPI camel
case names. `AgentHttpResponse.Message` and `Module` are nullable; IDs and
`ResponseType` are required. Do not model `provider`, `model`, `usage` or `rag`;
System.Text.Json will ignore them.

```csharp
internal sealed record AgentHttpRequest(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("conversationId")] Guid ConversationId,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("petId")] Guid? PetId,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles,
    [property: JsonPropertyName("isEscalated")] bool IsEscalated,
    [property: JsonPropertyName("correlationId")] Guid CorrelationId,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey,
    [property: JsonPropertyName("publishAsGlobalKnowledge")] bool PublishAsGlobalKnowledge);

internal sealed record AgentHttpResponse(
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("conversationId")] Guid ConversationId,
    [property: JsonPropertyName("correlationId")] Guid CorrelationId,
    [property: JsonPropertyName("responseType")] string ResponseType,
    [property: JsonPropertyName("module")] string? Module);
```

- [ ] **Step 5: Implement one-call HTTP behavior**

Construct one `HttpRequestMessage`, set Bearer with
`AuthenticationHeaderValue`, and use `JsonContent.Create` with web defaults.
Call:

```csharp
await httpClient.SendAsync(
    request,
    HttpCompletionOption.ResponseHeadersRead,
    cancellationToken);
```

For success, reject a declared `Content-Length` above the limit and then copy
the response stream in 8 KiB chunks into a `MemoryStream`, stopping with
`AgentContractException` before writing a chunk that would exceed
`MaxResponseBytes`. Deserialize once from that bounded stream and validate
returned IDs against the envelope:

```csharp
var declaredLength = response.Content.Headers.ContentLength;
if (declaredLength is > 0 && declaredLength.Value > options.MaxResponseBytes)
{
    throw new AgentContractException();
}

await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
await using var bounded = new MemoryStream();
var buffer = new byte[8192];
int read;
while ((read = await source.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0)
{
    if (bounded.Length + read > options.MaxResponseBytes)
    {
        throw new AgentContractException();
    }

    await bounded.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
}

bounded.Position = 0;
var payload = await JsonSerializer.DeserializeAsync<AgentHttpResponse>(
    bounded,
    serializerOptions,
    cancellationToken) ?? throw new AgentContractException();

if (payload.ConversationId != message.ConversationId ||
    payload.CorrelationId != message.CorrelationId)
{
    throw new AgentContractException();
}
```

Wrap only transport execution: propagate caller cancellation, translate a
timeout cancellation to `AgentTimeoutException`, and translate
`HttpRequestException` to `AgentUnavailableException`. Never include response
text, URL, JWT or SDK exception messages in a neutral exception message.

- [ ] **Step 6: Register enabled and disabled implementations**

Bind options first. Read the bound `AgentOptions` only to select registration:

```csharp
var agentOptions = configuration
    .GetSection(AgentOptions.SectionName)
    .Get<AgentOptions>() ?? new AgentOptions();

if (agentOptions.Enabled)
{
    services.AddHttpClient<IAgentMessagingClient, AgentMessagingHttpClient>((provider, client) =>
    {
        var validated = provider.GetRequiredService<IOptions<AgentOptions>>().Value;
        client.BaseAddress = new Uri(validated.BaseUrl, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(validated.RequestTimeoutSeconds);
    });
}
else
{
    services.AddSingleton<IAgentMessagingClient, DisabledAgentMessagingClient>();
}
```

The disabled implementation throws `AgentUnavailableException` without
creating `HttpClient`.

```csharp
public sealed class DisabledAgentMessagingClient : IAgentMessagingClient
{
    public Task<AgentMessageResult> SendAsync(
        AgentMessageEnvelope message,
        string accessToken,
        CancellationToken cancellationToken) =>
        Task.FromException<AgentMessageResult>(new AgentUnavailableException());
}
```

- [ ] **Step 7: Verify GREEN and commit**

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj `
  --filter FullyQualifiedName~Agent
git add src/Infrastructure/Agent src/Infrastructure/DependencyInjection.cs `
  tests/Infrastructure.Tests/Agent
git commit -m "feat: :sparkles: connect backend to agent API"
```

---

### Task 4: Authenticated public messages endpoint

**Files:**
- Create: `src/Api/Agent/Dtos/SendAgentMessageRequest.cs`
- Create: `src/Api/Agent/Dtos/SendAgentMessageResponse.cs`
- Create: `src/Api/Agent/Security/HttpContextUserAccessTokenProvider.cs`
- Create: `src/Api/Agent/Controllers/AgentMessagesController.cs`
- Create: `src/Api/Agent/DependencyInjection.cs`
- Modify: `src/Api/Program.cs`
- Modify: `src/Api/Common/Errors/GlobalExceptionHandler.cs`
- Test: `tests/Api.Tests/Agent/AgentMessagesHttpTests.cs`

**Interfaces:**
- Consumes: `SendAgentMessageCommand`, `IUserAccessTokenProvider`, MediatR, authenticated `person_id` and `role` claims.
- Produces: `POST /api/agent/messages` and safe Agent exception mappings.

- [ ] **Step 1: Write failing endpoint happy-path test**

Create `AgentApiFactory` with Oracle-free connection settings and valid RSA
settings, replace `IAgentMessagingClient` and `IConversationContextProvider`
with recording fakes, and issue an RS256 token containing:

```csharp
new Claim(JwtRegisteredClaimNames.Sub, AccountId.ToString()),
new Claim("person_id", PersonId.ToString()),
new Claim("role", "Cliente")
```

Test:

```csharp
[Fact]
public async Task Post_derives_identity_forwards_token_and_returns_reduced_response()
{
    using var client = factory.CreateAuthenticatedClient();
    using var request = new HttpRequestMessage(HttpMethod.Post, "/api/agent/messages")
    {
        Content = JsonContent.Create(new
        {
            message = "Necesito información",
            conversationId = (Guid?)null,
            petId = (Guid?)null,
            language = "es-CO"
        })
    };
    request.Headers.Add("Idempotency-Key", "message-001");
    request.Headers.Add("X-Correlation-ID", CorrelationId.ToString());

    using var response = await client.SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(PersonId, factory.AgentClient.Envelope!.UserId);
    Assert.Equal(["Cliente"], factory.AgentClient.Envelope.Roles);
    Assert.Equal(factory.AccessToken, factory.AgentClient.AccessToken);
    var json = await response.Content.ReadAsStringAsync();
    Assert.DoesNotContain("provider", json, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("rag", json, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Add failing security and contract tests**

Add exact tests:

```text
Post_without_token_returns_unauthorized_problem
Post_without_person_id_returns_unauthorized
Post_without_role_returns_unauthorized
Post_without_idempotency_header_returns_bad_request
Post_with_invalid_correlation_header_returns_bad_request
Post_without_correlation_header_generates_non_empty_id
Post_with_unknown_userId_property_returns_bad_request
Post_with_unknown_isEscalated_property_returns_bad_request
Post_with_invalid_body_returns_validation_violations
```

Use unique tests for `userId`, `roles`, `channel`, `isEscalated` and
`publishAsGlobalKnowledge`, or a `[Theory]` with one unknown property per case.

- [ ] **Step 3: Run endpoint tests and verify RED**

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj `
  --filter FullyQualifiedName~AgentMessagesHttpTests
```

Expected: 404 because the controller does not exist.

- [ ] **Step 4: Implement the reduced DTOs and token provider**

Use endpoint-specific unmapped-member rejection:

```csharp
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SendAgentMessageRequest(
    string Message,
    Guid? ConversationId,
    Guid? PetId,
    string Language);

public sealed record SendAgentMessageResponse(
    string? Message,
    Guid ConversationId,
    Guid CorrelationId,
    string ResponseType,
    string? Module);
```

`HttpContextUserAccessTokenProvider` reads
`IHttpContextAccessor.HttpContext.Request.Headers.Authorization`, requires one
Bearer value, returns only the parameter and otherwise throws
`UnauthorizedException("Authenticated access token is unavailable.")`.

```csharp
public string GetRequiredAccessToken()
{
    var raw = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
    if (!AuthenticationHeaderValue.TryParse(raw, out var authorization) ||
        !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrWhiteSpace(authorization.Parameter))
    {
        throw new UnauthorizedException("Authenticated access token is unavailable.");
    }

    return authorization.Parameter;
}
```

- [ ] **Step 5: Implement controller claim binding**

Use `[ApiController]`, `[Authorize]` and `[Route("api/agent/messages")]`. Parse
`person_id` as non-empty `Guid` and `role` as non-blank. Missing/invalid claims
throw `UnauthorizedException("Authenticated identity is invalid.")`. Map headers and body to
`SendAgentMessageCommand`; generate `Guid.NewGuid()` only when correlation is
absent. Return only `SendAgentMessageResponse`.

Use this action boundary so Swagger represents both headers accurately:

```csharp
[HttpPost]
public async Task<ActionResult<SendAgentMessageResponse>> Send(
    [FromBody] SendAgentMessageRequest request,
    [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
    [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId,
    CancellationToken cancellationToken)
{
    var personClaim = User.FindFirstValue("person_id");
    var role = User.FindFirstValue("role");
    if (!Guid.TryParse(personClaim, out var personId) || personId == Guid.Empty ||
        string.IsNullOrWhiteSpace(role))
    {
        throw new UnauthorizedException("Authenticated identity is invalid.");
    }

    var result = await sender.Send(new SendAgentMessageCommand(
        request.Message,
        request.ConversationId,
        request.PetId,
        request.Language,
        personId,
        role,
        idempotencyKey,
        correlationId ?? Guid.NewGuid()), cancellationToken);

    return Ok(new SendAgentMessageResponse(
        result.Message,
        result.ConversationId,
        result.CorrelationId,
        result.ResponseType,
        result.Module));
}
```

Add endpoint metadata for `200`, `400`, `401`, `403`, `409`, `502`, `503` and
`504` so Swagger documents the actual behavior.

- [ ] **Step 6: Register API token provider**

Create `AddAgentApi`:

```csharp
public static IServiceCollection AddAgentApi(this IServiceCollection services)
{
    services.AddHttpContextAccessor();
    services.AddScoped<IUserAccessTokenProvider, HttpContextUserAccessTokenProvider>();
    return services;
}
```

Call `builder.Services.AddAgentApi();` in `Program.cs` after Application and
Infrastructure registration.

- [ ] **Step 7: Map neutral exceptions centrally**

Extend `GlobalExceptionHandler.Map` to carry optional safe error code without
changing existing responses. Map:

```text
AgentAuthenticationException -> 502, agent_authentication_error
AgentContractException -> 502, agent_contract_error
AgentIdempotencyConflictException -> 409, agent_idempotency_conflict
AgentUnavailableException -> 503, agent_unavailable
AgentConversationCapacityException -> 503, agent_context_capacity_exhausted
AgentTimeoutException -> 504, agent_timeout
```

Add these arms before the fallback and return the third tuple value as the
non-TypeContract `error` value:

```csharp
AgentAuthenticationException authentication =>
    (StatusCodes.Status502BadGateway, authentication.Message, "agent_authentication_error"),
AgentContractException contract =>
    (StatusCodes.Status502BadGateway, contract.Message, "agent_contract_error"),
AgentIdempotencyConflictException conflict =>
    (StatusCodes.Status409Conflict, conflict.Message, "agent_idempotency_conflict"),
AgentUnavailableException unavailable =>
    (StatusCodes.Status503ServiceUnavailable, unavailable.Message, "agent_unavailable"),
AgentConversationCapacityException capacity =>
    (StatusCodes.Status503ServiceUnavailable, capacity.Message, "agent_context_capacity_exhausted"),
AgentTimeoutException timeout =>
    (StatusCodes.Status504GatewayTimeout, timeout.Message, "agent_timeout"),
```

Pass the code through the existing `error` property. Existing non-Agent
exceptions must retain their current reason phrase or TypeContract code.

- [ ] **Step 8: Verify API GREEN and commit**

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj `
  --filter FullyQualifiedName~AgentMessagesHttpTests
git add src/Api/Agent src/Api/Program.cs src/Api/Common/Errors/GlobalExceptionHandler.cs `
  tests/Api.Tests/Agent
git commit -m "feat: :sparkles: expose agent messages gateway"
```

---

### Task 5: Safe environment contract and operator documentation

**Files:**
- Modify: `.env.example`
- Modify: `README.md`
- Create: `tests/Api.Tests/Agent/AgentOptionsStartupTests.cs`

**Interfaces:**
- Consumes: `AgentOptions` property names and public endpoint contract.
- Produces: copy-safe local configuration and reproducible manual smoke steps.

- [ ] **Step 1: Sanitize and extend `.env.example`**

Ensure RSA material is blank:

```dotenv
Jwt__PrivateKeyPemBase64=
Jwt__PublicKeyPemBase64=
```

Add:

```dotenv
# Gateway interno hacia Huellitas ChatBot. En Docker use el nombre DNS del
# servicio, por ejemplo http://agent-api:8000.
Agent__Enabled=false
Agent__BaseUrl=http://localhost:8000
Agent__MessagesPath=/api/v1/messages
Agent__RequestTimeoutSeconds=30
Agent__ConversationContextTtlSeconds=900
Agent__ConversationContextCapacity=10000
Agent__MaxResponseBytes=1048576
```

- [ ] **Step 2: Document exact local execution**

Add to `README.md`:

```text
1. Start chatbot/Qdrant with docker compose in Huellitas_ChatBot.
2. Configure Agent__Enabled=true and Agent__BaseUrl=http://localhost:8000.
3. Start backend with dotnet run --project src/Api/Api.csproj --launch-profile http.
4. Obtain an access token from /api/auth/login or /api/auth/register.
5. Authorize Swagger and call POST /api/agent/messages with Idempotency-Key.
6. Reuse returned conversationId for subsequent messages.
```

State explicitly that generated conversations are transient until the
specialized persistence modules replace the provider.

- [ ] **Step 3: Verify startup option behavior**

Create `AgentOptionsStartupTests` with two independent service collections.
Register `AgentOptionsValidator`, bind an in-memory configuration and invoke
`IOptions<AgentOptions>.Value`:

```csharp
[Fact]
public void Disabled_agent_does_not_require_base_url()
{
    using var provider = BuildProvider(new Dictionary<string, string?>
    {
        ["Agent:Enabled"] = "false"
    });

    var options = provider.GetRequiredService<IOptions<AgentOptions>>().Value;

    Assert.False(options.Enabled);
}

[Fact]
public void Enabled_agent_with_empty_base_url_fails_validation()
{
    using var provider = BuildProvider(new Dictionary<string, string?>
    {
        ["Agent:Enabled"] = "true",
        ["Agent:BaseUrl"] = ""
    });

    var exception = Assert.Throws<OptionsValidationException>(
        () => _ = provider.GetRequiredService<IOptions<AgentOptions>>().Value);

    Assert.Contains(
        exception.Failures,
        failure => failure.Contains("Agent:BaseUrl", StringComparison.Ordinal));
}
```

`BuildProvider` must use `ConfigurationBuilder.AddInMemoryCollection`,
`AddSingleton<IValidateOptions<AgentOptions>, AgentOptionsValidator>()` and
`AddOptions<AgentOptions>().Bind(...).ValidateOnStart()`; it must not register
`VeterinaryDbContext`.

Run:

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj `
  --filter FullyQualifiedName~AgentOptionsStartupTests
```

- [ ] **Step 4: Commit the startup validation test**

```powershell
git add tests/Api.Tests/Agent/AgentOptionsStartupTests.cs
git commit -m "test: :white_check_mark: verify agent options startup"
```

- [ ] **Step 5: Run documentation safety searches**

```powershell
rg -n "BEGIN PRIVATE KEY|Jwt__PrivateKeyPemBase64=.+" .env.example README.md docs
rg -n "Agent__BaseUrl=" .env.example README.md docs
```

Expected: the first command has no matches; manually inspect the second output
and confirm every URL uses only `localhost` or the documented `agent-api` DNS.

- [ ] **Step 6: Commit Task 5 documentation**

```powershell
git add .env.example README.md
git commit -m "docs: :memo: document agent gateway configuration"
```

---

### Task 6: Full verification and optional real smoke test

**Files:**
- Verify only; modify files only to correct a failure found by these commands.

**Interfaces:**
- Consumes: complete gateway.
- Produces: evidence that the branch is ready for review.

- [ ] **Step 1: Run formatting verification**

```powershell
dotnet format veterinarian_backend.slnx --verify-no-changes
```

Expected: exit code 0 and no formatting changes required.

- [ ] **Step 2: Run all automated tests**

```powershell
dotnet test veterinarian_backend.slnx --configuration Debug
```

Expected: all Application, Infrastructure and API tests pass with zero failures.

- [ ] **Step 3: Build Release**

```powershell
dotnet build veterinarian_backend.slnx --configuration Release --no-restore
```

Expected: build succeeds with zero errors.

- [ ] **Step 4: Inspect the final diff and secret safety**

```powershell
git diff develop...HEAD --check
git status --short
git log --oneline develop..HEAD
rg -n "Bearer [A-Za-z0-9_-]+\.|BEGIN PRIVATE KEY|sk-[A-Za-z0-9]" `
  src tests docs .env.example README.md
```

Expected: clean diff, only intended commits and no token/private key/API key.

- [ ] **Step 5: Run opt-in manual smoke only with local services already active**

Do not automate real credentials. Configure the ignored `.env`, start the
backend and use Swagger with a freshly issued access token. Verify:

```text
first request with conversationId null -> 200 and non-empty conversationId
second request with returned conversationId -> 200 and same conversationId
same request and Idempotency-Key -> same agent result without duplicate model work
unknown userId property -> 400
chatbot stopped -> 503 agent_unavailable
```

- [ ] **Step 6: Commit only if verification required a correction**

Inspect `git status --short`, stage each corrected path explicitly rather than
using `git add -A`, and commit with:

```powershell
git commit -m "fix: :bug: complete agent gateway verification"
```

If no correction was needed, do not create an empty commit.
