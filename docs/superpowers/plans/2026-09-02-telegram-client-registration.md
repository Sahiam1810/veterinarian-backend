# Telegram Client Registration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que una persona inicie el registro en Telegram, verifique su correo con OTP, complete sus datos en una página HTTPS mínima y termine con una cuenta de cliente y el chat vinculados atómicamente.

**Architecture:** El backend extraerá la creación de cuentas de cliente a una operación reutilizable sin emisión de tokens. Un módulo `Telegram/Registration` administrará su propia máquina de estados, cifrado y persistencia; una aplicación MVC mínima consumirá el token de finalización y reutilizará la operación de registro dentro de la misma transacción que crea `TelegramUserLink`. El agente Python permanecerá fuera de todo el flujo de identidad.

**Tech Stack:** .NET 10, ASP.NET Core MVC, MediatR, FluentValidation, EF Core 10, Oracle 26ai, NSubstitute y xUnit.

## Global Constraints

- Trabajar directamente en `feature/telegram-client-registration`; no crear worktrees.
- Mantener el backend .NET como único propietario de identidad, credenciales, clientes y vínculos de Telegram.
- No enviar contraseña, identificación, OTP ni token de finalización al agente Python.
- No registrar correo, contraseña, identificación, OTP ni tokens en logs.
- Usar variables de entorno para URL, vencimientos, límites y clave criptográfica.
- Conservar `/api/auth/register`, `/vincular`, `/desvincular` y el modo invitado existentes.
- Ejecutar únicamente las pruebas dirigidas indicadas; no ejecutar toda la suite salvo petición expresa.
- Cada commit debe seguir Conventional Commits e incluir el emoji adecuado.

---

## File Map

### Registro reutilizable

- `src/Application/Security/Registration/ClientAccountRegistration.cs`: contratos de entrada, salida y puerto de creación sin tokens.
- `src/Infrastructure/Security/Authentication/ClientAccountRegistrationService.cs`: validación de unicidad y agregado de `Users`, `UserAccounts`, `UserCredentials` y `Clients` al contexto actual.
- `src/Infrastructure/Security/Authentication/AuthenticationService.cs`: reutiliza el nuevo servicio y conserva la emisión de JWT del registro público.

### Sesión y persistencia de Telegram

- `src/Domain/Telegram/Enums/TelegramRegistrationSessionStatus.cs`: estados del registro.
- `src/Domain/Telegram/Enums/TelegramRegistrationAccountKind.cs`: correo nuevo, cuenta activa o cuenta inactiva.
- `src/Domain/Telegram/Entities/TelegramRegistrationSession.cs`: invariantes, OTP y token de finalización.
- `src/Application/Telegram/Abstractions/ITelegramRegistrationSessionRepository.cs`: puerto de persistencia.
- `src/Infrastructure/Telegram/Repositories/TelegramRegistrationSessionRepository.cs`: consultas EF Core.
- `src/Infrastructure/Telegram/Configuration/TelegramRegistrationSessionConfiguration.cs`: mapeo Oracle.
- `src/Infrastructure/Migrations/*AddTelegramRegistrationSessions*`: migración generada por EF Core.

### Seguridad y configuración

- `src/Application/Telegram/Abstractions/ITelegramRegistrationProtector.cs`: generación/hash de token y protección del correo.
- `src/Infrastructure/Telegram/Security/TelegramRegistrationProtector.cs`: AES-GCM para correo y SHA-256 para token aleatorio.
- `src/Infrastructure/Telegram/Configuration/TelegramOptions.cs`: configuración del registro.
- `src/Infrastructure/Telegram/Configuration/TelegramOptionsValidator.cs`: validación de URL, límites y clave.
- `src/Application/Telegram/Abstractions/ITelegramRuntimeSettings.cs`: configuración consumida por Application.
- `src/Infrastructure/Telegram/Configuration/ConfiguredTelegramRuntimeSettings.cs`: adaptación de opciones.

### Flujo conversacional y finalización

- `src/Application/Telegram/Registration/TelegramRegistrationService.cs`: `/registrar`, correo y OTP.
- `src/Application/Telegram/Registration/CompleteTelegramRegistration.cs`: consulta de sesión y finalización transaccional.
- `src/Application/Telegram/Abstractions/ITelegramRegistrationAccountLookup.cs`: consulta de correo sin filtrar entidades de persistencia.
- `src/Infrastructure/Telegram/Identity/TelegramRegistrationAccountLookup.cs`: identifica cuenta nueva, activa o inactiva.
- `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs`: entrega los comandos de registro al nuevo servicio antes del modo invitado o agente.

### Página temporal

- `src/Api/Telegram/Controllers/TelegramRegistrationController.cs`: intercambio del token, formulario y resultado.
- `src/Api/Telegram/Dtos/CompleteTelegramRegistrationRequest.cs`: campos del formulario y validación superficial.
- `src/Api/Views/TelegramRegistration/Complete.cshtml`: formulario mínimo.
- `src/Api/Views/TelegramRegistration/Success.cshtml`: confirmación sin tokens.
- `src/Api/Program.cs`: MVC con vistas y antiforgery.
- `src/Api/Common/Security/RateLimitPolicies.cs`: política de finalización.
- `src/Api/Extensions/RateLimitingExtensions.cs`: límite específico del formulario.
- `.env.example`: documentación de variables.

---

### Task 1: Extraer la creación reutilizable de cuentas de cliente

**Files:**
- Create: `src/Application/Security/Registration/ClientAccountRegistration.cs`
- Create: `src/Infrastructure/Security/Authentication/ClientAccountRegistrationService.cs`
- Modify: `src/Infrastructure/Security/Authentication/AuthenticationService.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Create: `tests/Api.Tests/Security/ClientAccountRegistrationServiceTests.cs`
- Modify: `tests/Api.Tests/Security/AuthenticationServiceRegisterTests.cs`

**Interfaces:**
- Produces: `IClientAccountRegistrationService.StageAsync(ClientAccountRegistrationRequest, CancellationToken) -> Result<RegisteredClientAccount>`.
- `StageAsync` agrega entidades al `DbContext`, pero no abre transacción, no guarda cambios y no emite tokens.
- `RegisteredClientAccount` expone `PersonId`, `UserAccountId`, `RoleId`, `RoleName`, `FullName`, `UserName`, `Email` y `Status`.

- [ ] **Step 1: Escribir pruebas fallidas del servicio reutilizable**

```csharp
[Fact]
public async Task StageAsync_stages_user_account_credentials_and_client()
{
    var result = await sut.StageAsync(new ClientAccountRegistrationRequest(
        "Ana Cliente", "ana@huellitas.test", "ana.cliente",
        "Password123!", "1234567890"), default);

    Assert.True(result.IsSuccess);
    await users.Received(1).AddAsync(Arg.Any<UserEntity>(), default);
    await accounts.Received(1).AddAsync(Arg.Any<UserAccountEntity>(), default);
    await credentials.Received(1).AddAsync(Arg.Any<UserCredentialsEntity>(), default);
    await clients.Received(1).AddAsync(
        Arg.Is<ClientEntity>(x => x.UserId == result.Value.PersonId), default);
}

[Fact]
public async Task StageAsync_does_not_stage_entities_when_identification_is_used()
{
    clients.ExistsByIdentificationNumberAsync("1234567890", default).Returns(true);
    var result = await sut.StageAsync(ValidRequest, default);
    Assert.Equal(AuthenticationErrors.IdentificationNumberAlreadyExists, result.Error);
    await users.DidNotReceive().AddAsync(Arg.Any<UserEntity>(), default);
}
```

- [ ] **Step 2: Ejecutar las pruebas nuevas y confirmar el fallo**

Run:

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~ClientAccountRegistrationServiceTests
```

Expected: FAIL porque no existen `IClientAccountRegistrationService` ni `ClientAccountRegistrationService`.

- [ ] **Step 3: Crear los contratos de registro**

```csharp
public sealed record ClientAccountRegistrationRequest(
    string FullName,
    string Email,
    string UserName,
    string Password,
    string IdentificationNumber);

public sealed record RegisteredClientAccount(
    Guid PersonId,
    Guid UserAccountId,
    Guid RoleId,
    string RoleName,
    string FullName,
    string UserName,
    string Email,
    string Status);

public interface IClientAccountRegistrationService
{
    Task<Result<RegisteredClientAccount>> StageAsync(
        ClientAccountRegistrationRequest request,
        CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Mover la validación y creación de entidades al servicio**

Implementar `ClientAccountRegistrationService` con las mismas normalizaciones y errores actuales. Debe construir las cuatro entidades y devolver sus identificadores, sin llamar `SaveChangesAsync` ni `ExecuteInTransactionAsync`.

```csharp
var user = new UserEntity(fullName, normalizedEmail, passwordHash, clientRole.Id);
var account = new UserAccountEntity(user.Id, normalizedUserName, normalizedEmail, "Activo");
var credential = new UserCredentialEntity(account.Id, passwordHash);
var client = new ClientEntity(user.Id, identificationNumber, address: null);
```

- [ ] **Step 5: Adaptar `AuthenticationService.RegisterAsync`**

Dentro de su transacción existente, llamar `StageAsync`, convertir `RegisteredClientAccount` a `AuthenticatedIdentity` y conservar `IssueTokensAsync`. Un resultado fallido debe salir sin emitir tokens.

- [ ] **Step 6: Registrar la dependencia y ejecutar pruebas dirigidas**

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~ClientAccountRegistrationServiceTests|FullyQualifiedName~AuthenticationServiceRegisterTests"
```

Expected: PASS en las pruebas del servicio y del registro público existente.

- [ ] **Step 7: Commit**

```powershell
git add src/Application/Security/Registration src/Infrastructure/Security/Authentication src/Infrastructure/DependencyInjection.cs tests/Api.Tests/Security
git commit -m "refactor(security): 🔨 extract client account registration"
```

---

### Task 2: Modelar la sesión de registro de Telegram

**Files:**
- Create: `src/Domain/Telegram/Enums/TelegramRegistrationSessionStatus.cs`
- Create: `src/Domain/Telegram/Enums/TelegramRegistrationAccountKind.cs`
- Create: `src/Domain/Telegram/Entities/TelegramRegistrationSession.cs`
- Create: `tests/Application.Tests/Telegram/Domain/TelegramRegistrationSessionTests.cs`

**Interfaces:**
- Produces: `Start`, `PrepareOtp`, `RegisterFailedOtp`, `VerifyOtp`, `IssueCompletionToken`, `Complete`, `Cancel` y `Expire`.
- Estados válidos: `AwaitingEmail`, `AwaitingOtp`, `AwaitingProfile`, `Completed`, `Cancelled`, `Expired`, `Blocked`.
- Tipos de cuenta: `New`, `Active`, `Inactive`.

- [ ] **Step 1: Escribir pruebas fallidas de transiciones críticas**

```csharp
[Fact]
public void Correct_otp_moves_new_account_to_awaiting_profile()
{
    var session = TelegramRegistrationSession.Start(1001, 1001, Now);
    session.PrepareOtp(ProtectedEmail, EmailHash, OtpHash,
        TelegramRegistrationAccountKind.New, null, Now.AddMinutes(5), Now);
    session.VerifyOtp(Now.AddMinutes(1));
    session.IssueCompletionToken(TokenHash, Now.AddMinutes(16), Now.AddMinutes(1));
    Assert.Equal(TelegramRegistrationSessionStatus.AwaitingProfile, session.Status);
}

[Fact]
public void Maximum_failed_attempt_blocks_session_and_clears_otp()
{
    var session = OtpSession();
    session.RegisterFailedOtp(1, Now.AddSeconds(1));
    Assert.Equal(TelegramRegistrationSessionStatus.Blocked, session.Status);
    Assert.Null(session.OtpHash);
}

[Fact]
public void Completion_token_can_only_be_consumed_once()
{
    var session = ProfileSession();
    session.Complete(PersonId, Now.AddMinutes(2));
    Assert.Throws<InvalidOperationException>(() => session.Complete(PersonId, Now.AddMinutes(3)));
}
```

- [ ] **Step 2: Ejecutar la clase y confirmar el fallo**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~TelegramRegistrationSessionTests
```

Expected: FAIL porque la entidad y los enums no existen.

- [ ] **Step 3: Implementar enums, propiedades e invariantes**

La entidad debe exponer de solo lectura:

```csharp
Guid Id;
long TelegramUserId;
long TelegramChatId;
Guid? PersonId;
string? ProtectedEmail;
string? EmailHash;
string? OtpHash;
string? CompletionTokenHash;
TelegramRegistrationAccountKind AccountKind;
TelegramRegistrationSessionStatus Status;
int Attempts;
DateTime? OtpExpiresAt;
DateTime? CompletionExpiresAt;
```

Limpiar `OtpHash` al verificar, bloquear, cancelar o expirar; limpiar `CompletionTokenHash` al completar, cancelar o expirar. Rechazar identificadores vacíos, hashes que no tengan 64 caracteres y vencimientos no futuros.

- [ ] **Step 4: Ejecutar las pruebas dirigidas**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~TelegramRegistrationSessionTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/Domain/Telegram tests/Application.Tests/Telegram/Domain/TelegramRegistrationSessionTests.cs
git commit -m "feat(telegram): ✨ model client registration sessions"
```

---

### Task 3: Persistir sesiones de registro en Oracle

**Files:**
- Create: `src/Application/Telegram/Abstractions/ITelegramRegistrationSessionRepository.cs`
- Create: `src/Infrastructure/Telegram/Repositories/TelegramRegistrationSessionRepository.cs`
- Create: `src/Infrastructure/Telegram/Configuration/TelegramRegistrationSessionConfiguration.cs`
- Modify: `src/Application/Telegram/Abstractions/ITelegramUnitOfWork.cs`
- Modify: `src/Infrastructure/Telegram/TelegramUnitOfWork.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Generate: `src/Infrastructure/Migrations/*AddTelegramRegistrationSessions*.cs`
- Modify: `src/Infrastructure/Migrations/VeterinaryDbContextModelSnapshot.cs`
- Modify: `tests/Infrastructure.Tests/Telegram/TelegramPersistenceTests.cs`

**Interfaces:**
- Produces: `GetActiveByTelegramUserIdAsync`, `GetByCompletionTokenHashAsync`, `AddAsync` y `UpdateAsync`.
- La tabla Oracle será `TELEGRAM_REGISTRATION_SESSIONS`.

- [ ] **Step 1: Agregar pruebas fallidas de mapeo y repositorio**

Verificar que el modelo utiliza la tabla esperada, que `COMPLETION_TOKEN_HASH` tiene índice único y que la consulta activa solo considera `AwaitingEmail`, `AwaitingOtp` y `AwaitingProfile`.

```csharp
var entity = context.Model.FindEntityType(typeof(TelegramRegistrationSession));
Assert.Equal("TELEGRAM_REGISTRATION_SESSIONS", entity!.GetTableName());
Assert.Contains(entity.GetIndexes(), index =>
    index.IsUnique && index.Properties.Single().Name == nameof(TelegramRegistrationSession.CompletionTokenHash));
```

- [ ] **Step 2: Ejecutar la prueba de persistencia y confirmar el fallo**

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter FullyQualifiedName~TelegramPersistenceTests
```

Expected: FAIL por ausencia del repositorio y configuración.

- [ ] **Step 3: Implementar puerto, repositorio, mapeo y Unit of Work**

Usar columnas Oracle explícitas, `VARCHAR2(36)` para GUID, `VARCHAR2(64)` para hashes, `VARCHAR2(2048)` para correo protegido, `VARCHAR2(24)` para enums y `TIMESTAMP` para vencimientos. Añadir:

```csharp
ITelegramRegistrationSessionRepository RegistrationSessionsRepository { get; }
```

a `ITelegramUnitOfWork` y su implementación.

- [ ] **Step 4: Generar la migración EF Core**

```powershell
dotnet ef migrations add AddTelegramRegistrationSessions --project src/Infrastructure --startup-project src/Api --output-dir Migrations
```

Inspeccionar que la migración solo cree `TELEGRAM_REGISTRATION_SESSIONS`, sus índices y la FK opcional a `USERS`.

- [ ] **Step 5: Ejecutar las pruebas dirigidas y validar el modelo**

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter FullyQualifiedName~TelegramPersistenceTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/Application/Telegram/Abstractions src/Infrastructure/Telegram src/Infrastructure/DependencyInjection.cs src/Infrastructure/Migrations tests/Infrastructure.Tests/Telegram/TelegramPersistenceTests.cs
git commit -m "feat(telegram): ✨ persist registration sessions"
```

---

### Task 4: Proteger el correo y configurar el registro

**Files:**
- Create: `src/Application/Telegram/Abstractions/ITelegramRegistrationProtector.cs`
- Create: `src/Infrastructure/Telegram/Security/TelegramRegistrationProtector.cs`
- Modify: `src/Infrastructure/Telegram/Configuration/TelegramOptions.cs`
- Modify: `src/Infrastructure/Telegram/Configuration/TelegramOptionsValidator.cs`
- Modify: `src/Application/Telegram/Abstractions/ITelegramRuntimeSettings.cs`
- Modify: `src/Infrastructure/Telegram/Configuration/ConfiguredTelegramRuntimeSettings.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Create: `tests/Infrastructure.Tests/Telegram/TelegramRegistrationProtectorTests.cs`
- Modify: `tests/Api.Tests/Telegram/TelegramOptionsStartupTests.cs`

**Interfaces:**
- Produces: `GenerateCompletionToken`, `HashCompletionToken`, `ProtectEmail` y `UnprotectEmail`.
- La clave `RegistrationProtectionKeyBase64` debe decodificar exactamente 32 bytes.

- [ ] **Step 1: Escribir pruebas criptográficas y de opciones**

```csharp
[Fact]
public void Protected_email_round_trips_without_exposing_plaintext()
{
    var protectedEmail = sut.ProtectEmail("ana@huellitas.test");
    Assert.DoesNotContain("ana@huellitas.test", protectedEmail);
    Assert.Equal("ana@huellitas.test", sut.UnprotectEmail(protectedEmail));
}

[Fact]
public void Generated_completion_tokens_are_random_and_hash_to_sha256_hex()
{
    var first = sut.GenerateCompletionToken();
    var second = sut.GenerateCompletionToken();
    Assert.NotEqual(first, second);
    Assert.Equal(64, sut.HashCompletionToken(first).Length);
}
```

Agregar una prueba de inicio que rechace registro habilitado sin URL HTTPS o sin clave de 32 bytes.

- [ ] **Step 2: Ejecutar pruebas y confirmar el fallo**

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter FullyQualifiedName~TelegramRegistrationProtectorTests
dotnet test tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~TelegramOptionsStartupTests
```

- [ ] **Step 3: Implementar protección y configuración**

Añadir a `TelegramOptions`:

```csharp
bool RegistrationEnabled;
string RegistrationCompletionUrl;
int RegistrationOtpTtlMinutes = 10;
int RegistrationTokenTtlMinutes = 15;
int RegistrationMaxOtpAttempts = 3;
int RegistrationResendSeconds = 60;
string RegistrationProtectionKeyBase64;
```

Usar AES-GCM con nonce aleatorio de 12 bytes y tag de 16 bytes para `ProtectEmail`; empaquetar `nonce + tag + ciphertext` en Base64. Generar el token con 32 bytes aleatorios codificados Base64URL y persistir únicamente su SHA-256 hexadecimal.

- [ ] **Step 4: Ejecutar las pruebas dirigidas**

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter FullyQualifiedName~TelegramRegistrationProtectorTests
dotnet test tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~TelegramOptionsStartupTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/Application/Telegram/Abstractions src/Infrastructure/Telegram src/Infrastructure/DependencyInjection.cs tests/Infrastructure.Tests/Telegram/TelegramRegistrationProtectorTests.cs tests/Api.Tests/Telegram/TelegramOptionsStartupTests.cs
git commit -m "feat(telegram): 🔐 protect registration data"
```

---

### Task 5: Implementar `/registrar`, correo y OTP

**Files:**
- Create: `src/Application/Telegram/Abstractions/ITelegramRegistrationAccountLookup.cs`
- Create: `src/Infrastructure/Telegram/Identity/TelegramRegistrationAccountLookup.cs`
- Create: `src/Application/Telegram/Registration/TelegramRegistrationService.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Create: `tests/Application.Tests/Telegram/TelegramRegistrationServiceTests.cs`

**Interfaces:**
- Produces: `ITelegramRegistrationService.HandleAsync(TelegramInboundUpdate, CancellationToken) -> TelegramRegistrationOutcome`.
- `TelegramRegistrationOutcome` contiene `Consumed` y `Reply`.
- `ITelegramRegistrationAccountLookup.FindByEmailAsync` devuelve `AccountKind`, `PersonId` opcional y correo normalizado.

- [ ] **Step 1: Escribir pruebas fallidas de los caminos principales**

Cubrir estos casos dirigidos:

```csharp
[Fact] public async Task Register_command_starts_session_for_unlinked_private_chat();
[Fact] public async Task Email_submission_sends_generic_otp_reply_and_redacts_update();
[Fact] public async Task Valid_otp_for_new_email_returns_single_use_completion_link();
[Fact] public async Task Valid_otp_for_active_account_links_chat_without_creating_account();
[Fact] public async Task Invalid_otp_blocks_after_configured_attempts();
[Fact] public async Task Linked_chat_cannot_start_a_second_registration();
[Fact] public async Task Register_command_reuses_the_single_active_session();
```

En la prueba del enlace verificar solamente el prefijo configurado y que el token no se entregue a logs.

- [ ] **Step 2: Ejecutar la clase y confirmar el fallo**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~TelegramRegistrationServiceTests
```

- [ ] **Step 3: Implementar el lookup de cuenta**

Resolver por correo normalizado:

```csharp
public sealed record TelegramRegistrationAccount(
    TelegramRegistrationAccountKind Kind,
    Guid? PersonId,
    string NormalizedEmail);
```

Una cuenta activa devuelve `Active`; una cuenta existente no activa devuelve `Inactive`; la ausencia completa devuelve `New`.

- [ ] **Step 4: Implementar la máquina conversacional**

Orden de manejo:

```text
/cancelar -> cancela sesión de registro activa
/registrar -> valida chat privado, vínculo y configuración; inicia sesión
AwaitingEmail -> normaliza correo, prepara y envía OTP, redacta update
AwaitingOtp -> compara hash en tiempo constante y redacta update
Active -> crea o reactiva TelegramUserLink y completa
Inactive -> completa sin vínculo y responde que requiere soporte
New -> emite token, cambia a AwaitingProfile y devuelve RegistrationCompletionUrl
```

Usar la misma respuesta genérica de envío tanto para correo nuevo como existente.

- [ ] **Step 5: Ejecutar las pruebas dirigidas**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~TelegramRegistrationServiceTests
```

Expected: PASS en los siete casos dirigidos.

- [ ] **Step 6: Commit**

```powershell
git add src/Application/Telegram src/Infrastructure/Telegram src/Infrastructure/DependencyInjection.cs tests/Application.Tests/Telegram/TelegramRegistrationServiceTests.cs
git commit -m "feat(telegram): ✨ add conversational client registration"
```

---

### Task 6: Completar registro y vínculo en una transacción

**Files:**
- Create: `src/Application/Telegram/Registration/CompleteTelegramRegistration.cs`
- Create: `src/Application/Telegram/Registration/GetTelegramRegistrationSession.cs`
- Create: `src/Application/Telegram/Registration/CompleteTelegramRegistrationValidator.cs`
- Modify: `src/Application/Telegram/Errors/TelegramExceptions.cs`
- Create: `tests/Application.Tests/Telegram/CompleteTelegramRegistrationHandlerTests.cs`

**Interfaces:**
- Produces: `GetTelegramRegistrationSessionQuery(string Token)` para validar la página.
- Produces: `CompleteTelegramRegistrationCommand(string Token, string FullName, string IdentificationNumber, string UserName, string Password, string PasswordConfirmation)` como `IRequest<Result<CompletedTelegramRegistration>>`.
- El resultado exitoso contiene `CompletedTelegramRegistration(Guid PersonId, long TelegramChatId)`; los conflictos conservan el código de `AuthenticationErrors` para que la página marque el campo corregible.

- [ ] **Step 1: Escribir pruebas fallidas de finalización**

```csharp
[Fact]
public async Task Complete_stages_account_links_chat_and_consumes_session_in_one_transaction()
{
    var result = await handler.Handle(ValidCommand, default);
    Assert.True(result.IsSuccess);
    Assert.Equal(PersonId, result.Value.PersonId);
    await registration.Received(1).StageAsync(Arg.Any<ClientAccountRegistrationRequest>(), default);
    await links.Received(1).AddAsync(
        Arg.Is<TelegramUserLink>(x => x.PersonId == PersonId), default);
    Assert.Equal(TelegramRegistrationSessionStatus.Completed, session.Status);
}

[Fact] public async Task Reused_token_is_rejected_without_staging_account();
[Fact] public async Task Expired_token_is_rejected_without_staging_account();
[Fact] public async Task Registration_conflict_keeps_session_awaiting_profile();
[Fact] public async Task Password_confirmation_mismatch_fails_validation();
```

- [ ] **Step 2: Ejecutar las pruebas y confirmar el fallo**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~CompleteTelegramRegistrationHandlerTests
```

- [ ] **Step 3: Implementar consulta, validador y handler**

El handler debe:

```text
1. calcular el hash del token;
2. buscar AwaitingProfile por hash;
3. validar expiración;
4. descifrar el correo verificado;
5. abrir ITelegramUnitOfWork.ExecuteInTransactionAsync;
6. llamar IClientAccountRegistrationService.StageAsync;
7. crear o reactivar TelegramUserLink;
8. completar TelegramRegistrationSession;
9. confirmar la transacción;
10. devolver PersonId y TelegramChatId.
```

El validador debe reutilizar los mismos máximos de nombre, usuario, contraseña e identificación del registro público y exigir igualdad de contraseñas.

- [ ] **Step 4: Ejecutar las pruebas dirigidas**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~CompleteTelegramRegistrationHandlerTests
```

Expected: PASS en cinco casos.

- [ ] **Step 5: Commit**

```powershell
git add src/Application/Telegram tests/Application.Tests/Telegram/CompleteTelegramRegistrationHandlerTests.cs
git commit -m "feat(telegram): ✨ complete client registration atomically"
```

---

### Task 7: Servir la página mínima segura

**Files:**
- Create: `src/Api/Telegram/Controllers/TelegramRegistrationController.cs`
- Create: `src/Api/Telegram/Dtos/CompleteTelegramRegistrationRequest.cs`
- Create: `src/Api/Views/TelegramRegistration/Complete.cshtml`
- Create: `src/Api/Views/TelegramRegistration/Success.cshtml`
- Modify: `src/Api/Program.cs`
- Modify: `src/Api/Common/Security/RateLimitPolicies.cs`
- Modify: `src/Api/Extensions/RateLimitingExtensions.cs`
- Create: `tests/Api.Tests/Telegram/TelegramRegistrationControllerTests.cs`

**Interfaces:**
- Produces la página temporal en `/telegram/registration/complete`.
- Cookie: `__Host-HuellitasTelegramRegistration` en producción y `HuellitasTelegramRegistration` en desarrollo local.
- El `POST` usa antiforgery y nunca devuelve tokens.

- [ ] **Step 1: Escribir pruebas fallidas del controlador**

Cubrir:

```csharp
[Fact] public async Task Token_query_is_exchanged_for_http_only_cookie_and_clean_redirect();
[Fact] public async Task Valid_cookie_renders_form_without_raw_email_or_token();
[Fact] public async Task Successful_post_renders_confirmation_without_jwt();
[Fact] public async Task Invalid_or_expired_token_returns_gone_page();
```

Verificar `HttpOnly`, `SameSite=Strict`, expiración corta y `Secure` fuera de Development.

- [ ] **Step 2: Ejecutar pruebas y confirmar el fallo**

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~TelegramRegistrationControllerTests
```

- [ ] **Step 3: Habilitar MVC y antiforgery**

Cambiar a:

```csharp
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-HuellitasAntiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
```

Mantener Swagger, hubs y controladores existentes.

- [ ] **Step 4: Implementar controlador y vistas sin JavaScript**

El primer `GET` valida el token, lo escribe en cookie y redirige al mismo path sin query. El segundo `GET` valida la cookie y muestra el formulario. El `POST` usa `[ValidateAntiForgeryToken]`, envía el comando y elimina la cookie tanto al completar como al detectar token consumido.

Las vistas deben usar codificación Razor normal, `autocomplete` apropiado y no incluir correo, token, OTP ni identificación en mensajes de error globales.

- [ ] **Step 5: Añadir rate limiting específico**

Crear `RateLimitPolicies.TelegramRegistration` y reutilizar los valores conservadores de `Register` para intercambio, render y envío del formulario, particionados por IP.

- [ ] **Step 6: Ejecutar pruebas dirigidas**

```powershell
dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~TelegramRegistrationControllerTests|FullyQualifiedName~RateLimitingTests"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/Api/Telegram src/Api/Views/TelegramRegistration src/Api/Program.cs src/Api/Common/Security/RateLimitPolicies.cs src/Api/Extensions/RateLimitingExtensions.cs tests/Api.Tests/Telegram/TelegramRegistrationControllerTests.cs
git commit -m "feat(telegram): ✨ add secure registration page"
```

---

### Task 8: Integrar el registro antes del invitado y documentar operación

**Files:**
- Modify: `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs`
- Modify: `tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs`
- Modify: `.env.example`
- Modify: `README.md`

**Interfaces:**
- Consumes: `ITelegramRegistrationService.HandleAsync` de Task 5.
- Conserva el orden `link code -> registration -> linking -> linked/guest/agent`.

- [ ] **Step 1: Escribir dos pruebas fallidas de integración**

```csharp
[Fact]
public async Task Registration_message_is_consumed_before_guest_or_agent()
{
    registration.HandleAsync(Arg.Any<TelegramInboundUpdate>(), default)
        .Returns(new TelegramRegistrationOutcome(true, "Escribe tu correo."));
    await handler.Handle(new ProcessTelegramUpdateCommand(UpdateId), default);
    await bot.Received(1).SendTextAsync(ChatId, "Escribe tu correo.", default);
    await dispatcher.DidNotReceive().DispatchAsync(
        Arg.Any<AgentMessageDispatchRequest>(), Arg.Any<AgentConversationContext>(),
        Arg.Any<string>(), default);
}

[Fact]
public async Task Ordinary_guest_message_still_reaches_agent_when_registration_does_not_consume_it();
```

- [ ] **Step 2: Ejecutar las pruebas y confirmar el fallo**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~ProcessTelegramUpdateHandlerTests
```

- [ ] **Step 3: Inyectar y ejecutar el servicio de registro**

Después de procesar un link code y antes de `linkingService.HandleAsync`, ejecutar `registrationService.HandleAsync`. Si consume el mensaje, entregar su respuesta y terminar sin llamar al agente. Este orden permite que `/cancelar` cancele primero una sesión de registro; cuando no exista una, el servicio devolverá `Consumed=false` y el flujo de vinculación conservará su comportamiento actual.

- [ ] **Step 4: Documentar variables de entorno**

Añadir a `.env.example`:

```env
Telegram__RegistrationEnabled=false
Telegram__RegistrationCompletionUrl=https://example.com/telegram/registration/complete
Telegram__RegistrationOtpTtlMinutes=10
Telegram__RegistrationTokenTtlMinutes=15
Telegram__RegistrationMaxOtpAttempts=3
Telegram__RegistrationResendSeconds=60
Telegram__RegistrationProtectionKeyBase64=
```

Documentar en `README.md` generación local de la clave:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

y el recorrido `/registrar -> correo -> OTP -> enlace -> formulario -> chat vinculado`.

- [ ] **Step 5: Ejecutar verificación final acotada**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~TelegramRegistration|FullyQualifiedName~ProcessTelegramUpdateHandlerTests"
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~TelegramRegistration|FullyQualifiedName~TelegramPersistenceTests"
dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~ClientAccountRegistration|FullyQualifiedName~AuthenticationServiceRegisterTests|FullyQualifiedName~TelegramRegistration|FullyQualifiedName~TelegramOptionsStartupTests"
dotnet build veterinarian_backend.slnx --no-restore
git diff --check
```

Expected: todas las pruebas filtradas y el build pasan; `git diff --check` no reporta errores.

- [ ] **Step 6: Commit**

```powershell
git add src/Application/Telegram/Processing/ProcessTelegramUpdate.cs tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs .env.example README.md
git commit -m "feat(telegram): ✨ enable client onboarding flow"
```

---

## Final Manual Smoke Test

Con Oracle migrado, SMTP configurado, backend activo y webhook vigente:

```text
1. Abrir un chat privado no vinculado.
2. Enviar /registrar.
3. Enviar un correo nuevo y comprobar que el mensaje se redacta en la tabla de updates.
4. Introducir un OTP incorrecto una vez y luego el correcto.
5. Abrir el enlace HTTPS y comprobar que la URL queda limpia.
6. Completar nombre, identificación, usuario y contraseña.
7. Confirmar en Oracle la existencia de Users, UserAccounts, UserCredentials, Clients y TelegramUserLinks.
8. Enviar “¿Qué mascotas tengo?” y confirmar que responde como cliente vinculado sin pedir /vincular.
9. Reabrir el enlace anterior y confirmar que no permite reutilizarlo.
10. Repetir /registrar con una cuenta existente y confirmar que OTP termina en vinculación, no en duplicación.
```
