# Telegram Identity Conflict Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recuperar de forma segura una solicitud privada de Telegram cuando el correo verificado ya pertenece a una cuenta, sin perder el mensaje original ni vincular una cédula incorrecta.

**Architecture:** El agregado de sesión conservará la solicitud pendiente mientras reinicia únicamente el desafío de identidad. Application distinguirá cuentas nuevas, activas e inactivas y delegará la finalización del perfil a un puerto; Infrastructure completará un perfil `Client` faltante o rechazará una cédula diferente sin modificar datos existentes.

**Tech Stack:** .NET 10, C#, MediatR/application services existentes, EF Core con Oracle, xUnit y NSubstitute.

## Global Constraints

- No modificar endpoints, contratos HTTP, tablas, migraciones ni seeders.
- No vincular una cuenta activa con una cédula diferente a la de su perfil `Client`.
- No registrar ni exponer cédula, correo, OTP, JWT o mensaje privado.
- Conservar y reanudar exactamente una vez la solicitud privada original.
- Ejecutar solo compilación y pruebas enfocadas del flujo Telegram.

---

## Baseline

- Módulo/entidad/tabla: `Telegram` / `TelegramIdentitySession` / `TELEGRAM_IDENTITY_SESSIONS`.
- Git: rama `fix/telegram-stale-identity-access`, limpia al iniciar el análisis.
- Build: `dotnet build veterinarian_backend.slnx --no-restore` — correcto, 0 advertencias y 0 errores.
- Tests: 8 pruebas de `TelegramIdentityAccessServiceTests` y 4 de `TelegramClientIdentityGatewayTests` aprobadas.
- Migración/snapshot: sin cambios requeridos.
- Compatibilidad: cambio de comportamiento interno; webhook y respuestas del agente mantienen sus contratos.

## Approved Change Contract

- Operación: cambio de ciclo de vida de sesión y finalización idempotente de perfil cliente.
- Actual: un conflicto de registro cancela la sesión y borra la solicitud pendiente.
- Deseado: recuperar `AwaitingIdentification`, limpiar los datos temporales y conservar la solicitud; completar `Client` cuando la cuenta activa verificada aún no lo tenga.
- Clasificación: `behavior-changing`, compatible con API y esquema.
- Datos: sin backfill; nunca actualizar una identificación existente.
- Migración: no generar ni aplicar.
- Riesgos destructivos/breaking aceptados: ninguno.

## Impact graph

| Layer | Required? | Files/symbols | Reason | Verification |
|---|---|---|---|---|
| Domain | Sí | `TelegramIdentitySession` | Nueva transición de recuperación sin perder el pendiente | pruebas unitarias del agregado |
| Application | Sí | servicio, puerto, errores y pruebas de identidad | Orquestar cuenta activa/inactiva y recuperación | pruebas enfocadas del servicio |
| Infrastructure | Sí | `TelegramClientIdentityGateway` y pruebas | Completar o resolver perfil cliente | pruebas enfocadas del gateway |
| Api | No | ninguno | El webhook y DTO no cambian | compilación de la solución |

### Task 1: Recuperación segura de la sesión

**Files:**
- Modify: `src/Domain/Telegram/Entities/TelegramIdentitySession.cs`
- Test: `tests/Application.Tests/Telegram/Domain/TelegramIdentitySessionTests.cs`

**Interfaces:**
- Produces: `RecoverIdentification(DateTime now)`, que exige `AwaitingOtp`, conserva `PendingInboundUpdateId` y `ProtectedPendingMessage`, y limpia persona, identificación, nombre, correo, OTP, intentos y vencimientos de acceso.

- [ ] **Step 1: Write the failing test**

Agregar una prueba que construya una sesión de registro con mensaje pendiente, invoque
`RecoverIdentification`, y compruebe literalmente `AwaitingIdentification`, pendiente `42`,
mensaje protegido conservado y todos los demás datos sensibles en `null`.

- [ ] **Step 2: Run test to verify it fails**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-build --filter "FullyQualifiedName~TelegramIdentitySessionTests.Registration_conflict_recovers_identification_without_losing_pending_request"
```

Expected: error de compilación porque `RecoverIdentification` todavía no existe.

- [ ] **Step 3: Write minimal implementation**

Implementar la transición explícita dentro del agregado, sin reutilizar `Cancel` ni
`ClearSensitiveState`, porque ambos eliminan el mensaje pendiente.

- [ ] **Step 4: Run test to verify it passes**

Ejecutar la misma prueba sin `--no-build`; debe aprobar.

- [ ] **Step 5: Commit**

```powershell
git add src/Domain/Telegram/Entities/TelegramIdentitySession.cs tests/Application.Tests/Telegram/Domain/TelegramIdentitySessionTests.cs
git commit -m "fix(telegram): 🐛 preserve pending request on identity retry"
```

### Task 2: Orquestación de cuenta existente y recuperación

**Files:**
- Modify: `src/Application/Telegram/Abstractions/ITelegramClientIdentityGateway.cs`
- Modify: `src/Application/Telegram/Errors/TelegramExceptions.cs`
- Modify: `src/Application/Telegram/Identity/TelegramIdentityAccessService.cs`
- Modify: `tests/Application.Tests/Telegram/TelegramIdentityAccessServiceTests.cs`

**Interfaces:**
- Consumes: `TelegramIdentitySession.RecoverIdentification(DateTime)`.
- Produces: `CompleteRegistrationAsync(TelegramClientRegistration, Guid? existingPersonId, CancellationToken)` y `TelegramClientIdentificationMismatchException`.

- [ ] **Step 1: Write failing service tests**

Agregar pruebas que demuestren:

- un identificador menor de cinco caracteres mantiene `AwaitingIdentification` y solicita una cédula válida;
- una cuenta activa detectada por correo conserva su `PersonId` durante el OTP;
- una cuenta inactiva recupera la identificación sin perder el pendiente;
- después de OTP válido se llama `CompleteRegistrationAsync` con la persona activa;
- una cédula diferente recupera `AwaitingIdentification` y no devuelve datos de verificación;
- un registro nuevo exitoso devuelve `ResumeInboundUpdateId=42` y el mensaje original.

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~TelegramIdentityAccessServiceTests"
```

Expected: fallos por el puerto, transición y ramas todavía ausentes.

- [ ] **Step 3: Implement minimal application behavior**

Inyectar el `ITelegramRegistrationAccountLookup` existente. En `ProcessEmailAsync`, consultar
el correo normalizado antes de emitir el OTP; conservar el `PersonId` solo para cuentas activas
y recuperar la identificación para cuentas inactivas. En `ProcessOtpAsync`, delegar en
`CompleteRegistrationAsync`; capturar `TelegramClientIdentificationMismatchException` y
`TelegramRegistrationConflictException`, recuperar la sesión y devolver una instrucción
accionable para escribir la cédula registrada o `/cancelar`.

- [ ] **Step 4: Run tests to verify they pass**

Ejecutar las pruebas enfocadas del servicio; todas deben aprobar.

- [ ] **Step 5: Commit**

```powershell
git add src/Application/Telegram tests/Application.Tests/Telegram/TelegramIdentityAccessServiceTests.cs
git commit -m "fix(telegram): 🐛 keep identity recovery flow actionable"
```

### Task 3: Finalización segura del perfil cliente

**Files:**
- Modify: `src/Infrastructure/Telegram/Identity/TelegramClientIdentityGateway.cs`
- Modify: `tests/Infrastructure.Tests/Telegram/TelegramClientIdentityGatewayTests.cs`

**Interfaces:**
- Implements: `CompleteRegistrationAsync(TelegramClientRegistration, Guid? existingPersonId, CancellationToken)`.

- [ ] **Step 1: Write failing gateway tests**

Agregar casos con valores literales para probar que:

- una cuenta activa sin `Client` recibe exactamente un nuevo perfil con su mismo `UserId`;
- una cuenta activa con la misma cédula se resuelve sin crear duplicados;
- una cuenta activa con cédula diferente lanza `TelegramClientIdentificationMismatchException`;
- una cédula perteneciente a otra persona también se rechaza;
- una cuenta nueva conserva la creación de `User`, `UserAccount` y `Client`.

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~TelegramClientIdentityGatewayTests"
```

Expected: fallos porque el gateway aún no implementa la finalización para persona existente.

- [ ] **Step 3: Implement minimal gateway behavior**

Si `existingPersonId` existe, volver a validar que usuario y cuenta estén activos. Consultar el
cliente por `UserId`: devolverlo si la cédula coincide, crear solo `Client` si falta y la cédula
está disponible, o lanzar el error de diferencia sin actualizar registros. Si no existe persona,
mantener la creación transaccional actual de los tres registros.

- [ ] **Step 4: Run focused verification**

```powershell
dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~TelegramClientIdentityGatewayTests"
dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~TelegramIdentity"
dotnet build veterinarian_backend.slnx --no-restore
git diff --check
```

Expected: pruebas y compilación correctas, sin advertencias nuevas ni cambios de esquema/API.

- [ ] **Step 5: Commit**

```powershell
git add src/Infrastructure/Telegram/Identity/TelegramClientIdentityGateway.cs tests/Infrastructure.Tests/Telegram/TelegramClientIdentityGatewayTests.cs
git commit -m "fix(telegram): 🐛 complete existing client identity safely"
```

## Final compatibility audit

- Comparar la compilación final con la línea base de 0 errores/advertencias.
- Confirmar que no existen migraciones, cambios de `DbContext`, controllers ni DTOs.
- Revisar que el pending update solo se consume tras `Verify` y `TakePendingInboundUpdate`.
- Confirmar que ninguna respuesta o log contiene identificación, correo u OTP.
- Rollback: revertir los tres commits funcionales; no hay reversión de base de datos.
- Riesgo restante: una colisión concurrente de unicidad seguirá degradando a recuperación guiada.
