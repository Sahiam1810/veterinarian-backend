# Telegram Email OTP Linking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que una cuenta existente de Huellitas se vincule permanentemente con un chat privado de Telegram mediante un OTP enviado por SMTP, sin solicitar contraseñas ni exponer datos sensibles.

**Architecture:** El módulo Telegram conserva la máquina de estados y su persistencia Oracle. La resolución de cuentas y la entrega SMTP se consumen mediante puertos de aplicación; el agente queda fuera del flujo OTP y solo recibe mensajes después de una vinculación válida.

**Tech Stack:** .NET 10, C#, MediatR, FluentValidation, EF Core 10, Oracle, `System.Net.Mail`, xUnit, NSubstitute.

## Global Constraints

- Trabajar sobre una rama nueva `feature/telegram-email-otp-linking`, sin worktree.
- Mantener la arquitectura modular de cuatro capas existente.
- No solicitar ni almacenar contraseñas de Huellitas en Telegram.
- No registrar cuentas nuevas desde Telegram.
- El OTP dura 5 minutos, permite 5 intentos y es de un solo uso.
- No persistir correo u OTP en texto claro después de procesarlos.
- Nunca incluir correo, OTP, credenciales SMTP, JWT ni texto sensible en logs.
- La vinculación en `TELEGRAM_USER_LINKS` no vence por la vigencia del JWT delegado.
- Ejecutar solo pruebas dirigidas por tarea y una verificación final acotada.
- Usar commits Conventional Commits con emoji.

---

## File map

**Domain**

- Create `src/Domain/Telegram/Entities/TelegramLinkingSession.cs`: invariantes y transiciones de la sesión OTP.
- Create `src/Domain/Telegram/Enums/TelegramLinkingSessionStatus.cs`: estados persistibles.
- Modify `src/Domain/Telegram/Entities/TelegramInboundUpdate.cs`: borrado inmediato de texto sensible.

**Application**

- Create `src/Application/Telegram/Abstractions/ITelegramLinkingSessionRepository.cs`: persistencia abstracta.
- Create `src/Application/Telegram/Abstractions/ITelegramAccountLookup.cs`: lectura mínima de una cuenta activa.
- Create `src/Application/Telegram/Abstractions/ITelegramVerificationCodeSender.cs`: entrega desacoplada.
- Create `src/Application/Telegram/Abstractions/ITelegramOtpProtector.cs`: generación y verificación segura.
- Create `src/Application/Telegram/Models/TelegramLinkableAccount.cs`: identidad mínima.
- Create `src/Application/Telegram/Linking/TelegramChatLinkingService.cs`: máquina de estados de chat.
- Modify `src/Application/Telegram/Abstractions/ITelegramRuntimeSettings.cs`: TTL, intentos y reenvío.
- Modify `src/Application/Telegram/Abstractions/ITelegramUnitOfWork.cs`: repositorio de sesiones.
- Modify `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs`: comandos y flujo OTP antes del agente.
- Modify `src/Application/Telegram/Errors/TelegramExceptions.cs`: errores seguros de configuración y entrega.

**Infrastructure**

- Create `src/Infrastructure/Telegram/Configuration/TelegramLinkingSessionConfiguration.cs`: mapping Oracle.
- Create `src/Infrastructure/Telegram/Repositories/TelegramLinkingSessionRepository.cs`: consultas de sesión activa.
- Create `src/Infrastructure/Telegram/Security/TelegramOtpProtector.cs`: HMAC-SHA256 con pepper.
- Create `src/Infrastructure/Telegram/Identity/TelegramAccountLookup.cs`: adaptador sobre cuentas/usuarios.
- Create `src/Infrastructure/Email/Configuration/EmailOptions.cs`: configuración SMTP.
- Create `src/Infrastructure/Email/Configuration/EmailOptionsValidator.cs`: validación al arranque.
- Create `src/Infrastructure/Email/SmtpTelegramVerificationCodeSender.cs`: adaptador SMTP.
- Modify `src/Infrastructure/Telegram/Configuration/TelegramOptions.cs`: opciones OTP.
- Modify `src/Infrastructure/Telegram/Configuration/TelegramOptionsValidator.cs`: validación OTP.
- Modify `src/Infrastructure/Telegram/Configuration/ConfiguredTelegramRuntimeSettings.cs`: valores resueltos.
- Modify `src/Infrastructure/Telegram/TelegramUnitOfWork.cs`: exposición del repositorio.
- Modify `src/Infrastructure/DependencyInjection.cs`: registros de puertos, adaptadores y opciones.
- Create via EF in `src/Infrastructure/Migrations`: migration named `TelegramEmailOtpLinking` para la tabla e índices Oracle.
- Modify `src/Infrastructure/Migrations/VeterinaryDbContextModelSnapshot.cs`: snapshot generado.

**Configuration and documentation**

- Modify `.env.example`: variables SMTP y OTP documentadas.
- Modify `docs/telegram-channel-setup.md`: comandos y operación del flujo.

**Focused tests**

- Modify `tests/Application.Tests/Telegram/Domain/TelegramEntitiesTests.cs`.
- Modify `tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs`.
- Create `tests/Application.Tests/Telegram/TelegramChatLinkingServiceTests.cs`.
- Modify `tests/Infrastructure.Tests/Telegram/TelegramPersistenceTests.cs`.
- Create `tests/Infrastructure.Tests/Telegram/TelegramOtpProtectorTests.cs`.
- Create `tests/Infrastructure.Tests/Telegram/SmtpTelegramVerificationCodeSenderTests.cs`.

---

### Task 1: Modelar la sesión OTP y el borrado sensible

**Files:**
- Create: `src/Domain/Telegram/Enums/TelegramLinkingSessionStatus.cs`
- Create: `src/Domain/Telegram/Entities/TelegramLinkingSession.cs`
- Modify: `src/Domain/Telegram/Entities/TelegramInboundUpdate.cs`
- Test: `tests/Application.Tests/Telegram/Domain/TelegramEntitiesTests.cs`

**Interfaces:**
- Produces: `TelegramLinkingSession.Start`, `ResolveAccount`, `RegisterFailedAttempt`, `Complete`, `Cancel`, `Expire` and `TelegramInboundUpdate.RedactSensitiveText`.

- [ ] **Step 1: Write the failing domain tests**

Add focused tests proving that a session starts in `AwaitingEmail`, moves to `AwaitingOtp`, blocks on the fifth invalid attempt, completes once, expires after its deadline, and that redaction replaces `MessageText` with `null` while processing remains possible.

```csharp
[Fact]
public void Linking_session_blocks_after_fifth_invalid_otp()
{
    var session = TelegramLinkingSession.Start(1001, 1001, Now);
    session.ResolveAccount(PersonId, "email-hash", "otp-hash", Now.AddMinutes(5), Now);

    for (var attempt = 0; attempt < 5; attempt++)
        session.RegisterFailedAttempt(5, Now.AddSeconds(attempt));

    Assert.Equal(TelegramLinkingSessionStatus.Blocked, session.Status);
}
```

- [ ] **Step 2: Run the focused tests and verify failure**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~TelegramEntitiesTests" --no-restore`

Expected: FAIL because the session types and redaction method do not exist.

- [ ] **Step 3: Implement the domain model**

Use an enum with `AwaitingEmail`, `AwaitingOtp`, `Linked`, `Cancelled`, `Expired`, and `Blocked`. The entity must contain only hashes and identifiers; transition methods reject invalid current states and update `UpdatedAt`.

```csharp
public void RedactSensitiveText(DateTime redactedAt)
{
    if (Status != TelegramInboundUpdateStatus.Processing)
        throw new InvalidOperationException("Solo se puede proteger una actualización en procesamiento.");

    MessageText = null;
    UpdatedAt = redactedAt;
}
```

- [ ] **Step 4: Run the domain tests**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~TelegramEntitiesTests" --no-restore`

Expected: PASS.

- [ ] **Step 5: Commit the domain slice**

```bash
git add src/Domain/Telegram tests/Application.Tests/Telegram/Domain/TelegramEntitiesTests.cs
git commit -m "feat: ✨ model Telegram OTP linking sessions"
```

### Task 2: Definir puertos y configurar la seguridad OTP

**Files:**
- Create: `src/Application/Telegram/Abstractions/ITelegramLinkingSessionRepository.cs`
- Create: `src/Application/Telegram/Abstractions/ITelegramAccountLookup.cs`
- Create: `src/Application/Telegram/Abstractions/ITelegramVerificationCodeSender.cs`
- Create: `src/Application/Telegram/Abstractions/ITelegramOtpProtector.cs`
- Create: `src/Application/Telegram/Models/TelegramLinkableAccount.cs`
- Modify: `src/Application/Telegram/Abstractions/ITelegramRuntimeSettings.cs`
- Modify: `src/Application/Telegram/Abstractions/ITelegramUnitOfWork.cs`
- Create: `src/Infrastructure/Telegram/Security/TelegramOtpProtector.cs`
- Modify: `src/Infrastructure/Telegram/Configuration/TelegramOptions.cs`
- Modify: `src/Infrastructure/Telegram/Configuration/TelegramOptionsValidator.cs`
- Modify: `src/Infrastructure/Telegram/Configuration/ConfiguredTelegramRuntimeSettings.cs`
- Test: `tests/Infrastructure.Tests/Telegram/TelegramOtpProtectorTests.cs`

**Interfaces:**
- Produces: `FindActiveByEmailAsync`, `SendAsync`, `Create`, `Verify`, `GetActiveByTelegramUserIdAsync`, `OtpLifetime`, `OtpMaximumAttempts`, and `OtpResendInterval`.

- [ ] **Step 1: Write failing OTP protector tests**

Cover a six-digit code, verification with the configured pepper, rejection of a different code, and deterministic verification without exposing the raw OTP.

```csharp
[Fact]
public void Protector_verifies_only_the_original_code()
{
    var protector = new TelegramOtpProtector(PepperBase64);
    var generated = protector.Create();

    Assert.Matches("^[0-9]{6}$", generated.Code);
    Assert.True(protector.Verify(generated.Code, generated.Hash));
    Assert.False(protector.Verify("000000", generated.Hash));
}
```

- [ ] **Step 2: Run the focused security test**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~TelegramOtpProtectorTests" --no-restore`

Expected: FAIL because the protector and contracts do not exist.

- [ ] **Step 3: Add ports and settings**

Define the minimal contracts:

```csharp
public sealed record TelegramLinkableAccount(Guid PersonId, string Email);
public sealed record GeneratedTelegramOtp(string Code, string Hash);

public interface ITelegramAccountLookup
{
    Task<TelegramLinkableAccount?> FindActiveByEmailAsync(string email, CancellationToken cancellationToken);
}

public interface ITelegramVerificationCodeSender
{
    Task SendAsync(string destination, string code, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}

public interface ITelegramOtpProtector
{
    GeneratedTelegramOtp Create();
    bool Verify(string code, string expectedHash);
    string HashEmail(string normalizedEmail);
}
```

Add `OtpLifetime`, `OtpMaximumAttempts`, and `OtpResendInterval` to runtime settings. Add `OtpTtlMinutes=5`, `OtpMaximumAttempts=5`, `OtpResendSeconds=60`, and `OtpPepperBase64` to `TelegramOptions`. Require a decoded pepper of at least 32 bytes when Telegram is enabled.

- [ ] **Step 4: Implement HMAC protection**

Generate the OTP with `RandomNumberGenerator.GetInt32(0, 1_000_000)`, format with `D6`, hash OTP and normalized email using HMAC-SHA256, and compare with `CryptographicOperations.FixedTimeEquals`.

- [ ] **Step 5: Run the security test**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~TelegramOtpProtectorTests" --no-restore`

Expected: PASS.

- [ ] **Step 6: Commit the contracts and protection**

```bash
git add src/Application/Telegram src/Infrastructure/Telegram/Configuration src/Infrastructure/Telegram/Security tests/Infrastructure.Tests/Telegram/TelegramOtpProtectorTests.cs
git commit -m "feat: 🔐 add Telegram OTP security ports"
```

### Task 3: Persistir sesiones OTP en Oracle

**Files:**
- Create: `src/Infrastructure/Telegram/Configuration/TelegramLinkingSessionConfiguration.cs`
- Create: `src/Infrastructure/Telegram/Repositories/TelegramLinkingSessionRepository.cs`
- Modify: `src/Infrastructure/Telegram/TelegramUnitOfWork.cs`
- Create via EF: `src/Infrastructure/Migrations/*_TelegramEmailOtpLinking.cs`
- Modify: `src/Infrastructure/Migrations/VeterinaryDbContextModelSnapshot.cs`
- Modify: `tests/Infrastructure.Tests/Telegram/TelegramPersistenceTests.cs`

**Interfaces:**
- Consumes: `ITelegramLinkingSessionRepository` and `TelegramLinkingSession` from Tasks 1–2.
- Produces: Oracle table `TELEGRAM_LINKING_SESSIONS` and active-session lookup.

- [ ] **Step 1: Write the failing persistence metadata test**

Assert table name, `NUMBER(19)` external IDs, `VARCHAR2(64)` hashes, status conversion, and a unique filtered-equivalent lookup index on Telegram user and status-compatible fields.

- [ ] **Step 2: Run only the persistence test**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~TelegramPersistenceTests" --no-restore`

Expected: FAIL because the EF configuration is absent.

- [ ] **Step 3: Implement mapping and repository**

The repository signature is:

```csharp
Task<TelegramLinkingSession?> GetActiveByTelegramUserIdAsync(
    long telegramUserId,
    DateTime now,
    CancellationToken cancellationToken);
Task AddAsync(TelegramLinkingSession session, CancellationToken cancellationToken);
Task UpdateAsync(TelegramLinkingSession session, CancellationToken cancellationToken);
```

Query only `AwaitingEmail` and `AwaitingOtp`, and allow the domain service to expire a stale result. Register the repository in `TelegramUnitOfWork`.

- [ ] **Step 4: Generate and inspect the migration**

Run:

```powershell
dotnet ef migrations add TelegramEmailOtpLinking --project src/Infrastructure --startup-project src/Api
```

Verify that the migration creates only the session table and its indexes; it must not alter unrelated columns.

- [ ] **Step 5: Run the persistence test**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~TelegramPersistenceTests" --no-restore`

Expected: PASS.

- [ ] **Step 6: Commit persistence**

```bash
git add src/Infrastructure/Telegram src/Infrastructure/Migrations tests/Infrastructure.Tests/Telegram/TelegramPersistenceTests.cs
git commit -m "feat: 💾 persist Telegram OTP sessions"
```

### Task 4: Implementar el adaptador SMTP y la búsqueda de cuenta

**Files:**
- Create: `src/Infrastructure/Email/Configuration/EmailOptions.cs`
- Create: `src/Infrastructure/Email/Configuration/EmailOptionsValidator.cs`
- Create: `src/Infrastructure/Email/SmtpTelegramVerificationCodeSender.cs`
- Create: `src/Infrastructure/Telegram/Identity/TelegramAccountLookup.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Create: `tests/Infrastructure.Tests/Telegram/SmtpTelegramVerificationCodeSenderTests.cs`

**Interfaces:**
- Consumes: `ITelegramVerificationCodeSender`, `ITelegramAccountLookup`, `TelegramLinkableAccount`.
- Produces: SMTP delivery and active-account lookup adapters.

- [ ] **Step 1: Write focused adapter tests**

Use a substitutable internal `ISmtpClient` boundary so the test verifies recipient, neutral subject, expiration text, and disposal without making network calls. Add one options-validation test for missing password when email is enabled.

- [ ] **Step 2: Run the SMTP test and verify failure**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~SmtpTelegramVerificationCodeSenderTests" --no-restore`

Expected: FAIL because the email adapter does not exist.

- [ ] **Step 3: Implement options and SMTP delivery**

Bind `EmailOptions` to `Email` and validate `Host`, port range, credentials, sender address, and TLS. The message body must include the OTP and its expiration, but logger calls may include only a correlation identifier and outcome.

```csharp
public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public bool Enabled { get; init; }
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Huellitas";
    public bool UseTls { get; init; } = true;
}
```

- [ ] **Step 4: Implement account lookup without leaking repositories**

Use the existing `IUserAccountsRepository.GetByMailAsync` and the associated active user. Return only `PersonId` and normalized email. Do not expose account status, password hash, refresh tokens, or EF entities to Application Telegram.

- [ ] **Step 5: Register and validate dependencies**

Register options with `ValidateOnStart`, the SMTP adapter, account lookup, OTP protector, and session repository. When Telegram OTP linking is active, a disabled email provider is a startup configuration error.

- [ ] **Step 6: Run the SMTP test**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~SmtpTelegramVerificationCodeSenderTests" --no-restore`

Expected: PASS.

- [ ] **Step 7: Commit adapters**

```bash
git add src/Infrastructure/Email src/Infrastructure/Telegram/Identity src/Infrastructure/DependencyInjection.cs tests/Infrastructure.Tests/Telegram/SmtpTelegramVerificationCodeSenderTests.cs
git commit -m "feat: ✨ send Telegram verification codes by SMTP"
```

### Task 5: Implementar la máquina de estados de vinculación

**Files:**
- Create: `src/Application/Telegram/Linking/TelegramChatLinkingService.cs`
- Modify: `src/Application/Telegram/Errors/TelegramExceptions.cs`
- Create: `tests/Application.Tests/Telegram/TelegramChatLinkingServiceTests.cs`

**Interfaces:**
- Consumes: all linking ports, settings, `ITelegramUnitOfWork`, and `TimeProvider`.
- Produces: `HandleAsync(TelegramInboundUpdate update, CancellationToken)` returning whether the message was consumed and the safe Telegram reply.

- [ ] **Step 1: Write four focused state-machine tests**

Cover: `/vincular` starts a session; valid email sends an OTP and moves state; fifth invalid OTP blocks; valid OTP creates `TelegramUserLink` and completes. Reuse a fixture with substitutes rather than multiplying test cases.

```csharp
public sealed record TelegramLinkingOutcome(bool Consumed, string? Reply);

Task<TelegramLinkingOutcome> HandleAsync(
    TelegramInboundUpdate update,
    CancellationToken cancellationToken);
```

- [ ] **Step 2: Run only the new service tests**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~TelegramChatLinkingServiceTests" --no-restore`

Expected: FAIL because the service is absent.

- [ ] **Step 3: Implement command priority and generic responses**

Order: `/cancelar`, `/desvincular`, `/vincular`, active-session input, normal message. Do not invoke account lookup when no active session exists. Use the same generic reply after receiving an email regardless of account existence; for nonexistent accounts, persist a non-verifiable session state/hash so timing and copy remain indistinguishable without sending email.

- [ ] **Step 4: Apply throttling and conflict rules**

Reject re-send before `OtpResendInterval`; do not replace an existing link owned by another Telegram user; consume the OTP atomically with creating `TelegramUserLink`; redact the inbound update before saving after identifying email or OTP input.

- [ ] **Step 5: Run service tests**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~TelegramChatLinkingServiceTests" --no-restore`

Expected: PASS.

- [ ] **Step 6: Commit the application flow**

```bash
git add src/Application/Telegram tests/Application.Tests/Telegram/TelegramChatLinkingServiceTests.cs
git commit -m "feat: ✨ orchestrate Telegram OTP linking"
```

### Task 6: Integrar vinculación con el procesamiento de Telegram

**Files:**
- Modify: `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs`
- Modify: `tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs`

**Interfaces:**
- Consumes: `TelegramChatLinkingService.HandleAsync` from Task 5.
- Produces: linked users continue to the existing conversation/agent path; linking inputs never reach the agent.

- [ ] **Step 1: Add two failing integration-level handler tests**

Verify that a consumed linking message is delivered to Telegram without calling `IAgentMessageDispatcher`, and that a previously linked user still calls the dispatcher without starting a session.

- [ ] **Step 2: Run only handler tests**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~ProcessTelegramUpdateHandlerTests" --no-restore`

Expected: FAIL on the new linking behavior.

- [ ] **Step 3: Integrate before user-link resolution**

After chat-type and text validation, keep legacy `/start <code>` compatibility, then call the linking service. If `Consumed` is true, deliver its reply and return. Otherwise retain the current lookup, conversation resolution, delegated JWT, agent dispatch, and response delivery.

- [ ] **Step 4: Keep safe retry classification**

Map SMTP delivery errors to `verification_delivery_failed`; never use exception messages as persisted error codes. Existing Telegram delivery and agent retry behavior remains unchanged.

- [ ] **Step 5: Run handler tests**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~ProcessTelegramUpdateHandlerTests" --no-restore`

Expected: PASS.

- [ ] **Step 6: Commit integration**

```bash
git add src/Application/Telegram/Processing/ProcessTelegramUpdate.cs src/Application/Telegram/Errors/TelegramExceptions.cs tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs
git commit -m "feat: ✨ link Huellitas accounts from Telegram"
```

### Task 7: Documentar, migrate and verify the complete slice

**Files:**
- Modify: `.env.example`
- Modify: `docs/telegram-channel-setup.md`

**Interfaces:**
- Produces: reproducible local configuration and operator instructions.

- [ ] **Step 1: Document environment variables**

Add:

```env
Telegram__OtpTtlMinutes=5
Telegram__OtpMaximumAttempts=5
Telegram__OtpResendSeconds=60
Telegram__OtpPepperBase64=
Email__Enabled=true
Email__Host=
Email__Port=587
Email__Username=
Email__Password=
Email__FromAddress=
Email__FromName=Huellitas
Email__UseTls=true
```

Explain how to generate a 32-byte pepper in PowerShell without printing unrelated secrets:

```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

- [ ] **Step 2: Document operator flow**

Include `/vincular`, email, OTP, `/cancelar`, `/desvincular`, permanent link semantics, SMTP prerequisites, and a warning that `DelegatedTokenMinutes` is internal and does not log the user out of Telegram.

- [ ] **Step 3: Build without running the full test estate**

Run: `dotnet build veterinarian-backend.sln --no-restore`

Expected: build succeeds with zero errors.

- [ ] **Step 4: Run the bounded Telegram test set**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~Telegram" --no-build
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~Telegram" --no-build
```

Expected: all Telegram-focused tests pass. Do not run unrelated suites unless a failure indicates a shared regression.

- [ ] **Step 5: Apply migration in the configured development Oracle database**

Run:

```powershell
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

Expected: `TELEGRAM_LINKING_SESSIONS` exists and unrelated tables are unchanged.

- [ ] **Step 6: Perform one manual smoke flow**

Start the API visibly, keep the existing public webhook tunnel, send `/vincular`, enter a registered email, enter the received OTP, then send one veterinary question. Confirm one permanent user link, a completed linking session, and one agent call. Confirm logs contain no email, OTP, JWT, SMTP password, or user message.

- [ ] **Step 7: Commit documentation**

```bash
git add .env.example docs/telegram-channel-setup.md
git commit -m "docs: 📝 document Telegram OTP linking"
```

## Self-review results

- Spec coverage: persistent linking, email OTP, SMTP, modular boundaries, Oracle persistence, restart recovery, redaction, commands, throttling, conflicts, observability and bounded tests are mapped to Tasks 1–7.
- Placeholder scan: no behavioral placeholders or unspecified error handling remain; EF supplies only the conventional migration timestamp.
- Type consistency: linking contracts are introduced in Task 2, persisted in Task 3, adapted in Task 4, orchestrated in Task 5 and consumed by the existing handler in Task 6.
