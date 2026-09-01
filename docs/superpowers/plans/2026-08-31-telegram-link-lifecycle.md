# Telegram Link Lifecycle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Corregir la revinculacion OTP de chats revocados, exigir vinculacion antes de conversar y despertar el worker sin consultar Oracle cada cinco segundos.

**Architecture:** Se preserva el historial mediante indices unicos funcionales para vinculos activos. Application valida conflictos antes de persistir y publica una senal desacoplada tras ingerir un update; Infrastructure implementa la senal en memoria y mantiene un sondeo de respaldo de 30 segundos.

**Tech Stack:** .NET 10, C#, MediatR, EF Core, Oracle AI Database 26ai, xUnit, NSubstitute.

## Global Constraints

- Trabajar en `fix/telegram-link-lifecycle` sin worktree.
- No borrar filas historicas ni conversaciones al desvincular.
- No exponer correo, OTP, JWT, token del bot ni contenido del usuario en logs.
- Ejecutar pruebas dirigidas del modulo Telegram; evitar la suite completa salvo verificacion final imprescindible.
- No aplicar la migracion a Oracle sin confirmar primero el destino.

---

### Task 1: Validar y liberar vinculos revocados

**Files:**
- Modify: `tests/Application.Tests/Telegram/TelegramChatLinkingServiceTests.cs`
- Modify: `src/Application/Telegram/Linking/TelegramChatLinkingService.cs`

**Interfaces:**
- Consumes: `ITelegramUserLinkRepository.GetByPersonIdAsync`, `GetByTelegramUserIdAsync` y `GetByTelegramChatIdAsync`.
- Produces: finalizacion OTP que crea o reactiva solo cuando no existe otro vinculo activo incompatible.

- [ ] **Step 1: Escribir pruebas fallidas para chat revocado y conflicto activo**

Agregar casos que demuestren que una fila revocada del mismo chat no bloquea el nuevo `AddAsync`, mientras un chat activo de otra persona cancela la sesion, devuelve una respuesta controlada y no persiste un vinculo.

- [ ] **Step 2: Ejecutar las pruebas y confirmar RED**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~TelegramChatLinkingServiceTests" --no-restore --nologo`

Expected: FAIL porque `ProcessOtpAsync` solo consulta por persona y no detecta la ocupacion activa del chat.

- [ ] **Step 3: Implementar la validacion minima**

Consultar vinculos activos por usuario y chat antes de la transaccion. Aceptar el mismo `PersonId`; cancelar la sesion y devolver una respuesta generica ante una identidad distinta. Mantener `TelegramUserLink.Relink` para la fila historica de la misma persona y `Create` cuando no existe una fila para esa persona.

- [ ] **Step 4: Ejecutar las pruebas y confirmar GREEN**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~TelegramChatLinkingServiceTests" --no-restore --nologo`

Expected: PASS.

### Task 2: Representar unicidad solo para vinculos activos en Oracle

**Files:**
- Modify: `tests/Infrastructure.Tests/Telegram/TelegramPersistenceTests.cs`
- Modify: `src/Infrastructure/Telegram/Configuration/TelegramUserLinkConfiguration.cs`
- Create: `src/Infrastructure/Migrations/<timestamp>_TelegramActiveUserLinkIndexes.cs`
- Create: `src/Infrastructure/Migrations/<timestamp>_TelegramActiveUserLinkIndexes.Designer.cs`
- Modify: `src/Infrastructure/Migrations/VeterinaryDbContextModelSnapshot.cs`

**Interfaces:**
- Consumes: `TelegramUserLink.UnlinkedAt` como indicador de vigencia.
- Produces: indices Oracle `UX_TELEGRAM_USER_LINKS_PERSON`, `UX_TELEGRAM_USER_LINKS_USER` y `UX_TELEGRAM_USER_LINKS_CHAT` unicos solo cuando el vinculo esta activo.

- [ ] **Step 1: Cambiar la prueba de metadatos para rechazar indices unicos incondicionales**

La prueba debe comprobar que el modelo EF no declara indices unicos simples sobre persona, usuario o chat, porque Oracle los gestionara mediante expresiones condicionales en la migracion.

- [ ] **Step 2: Ejecutar la prueba y confirmar RED**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~TelegramPersistenceTests.Model_uses_approved_tables" --no-restore --nologo`

Expected: FAIL porque la configuracion actual declara tres indices unicos simples.

- [ ] **Step 3: Retirar los indices simples del modelo y generar una migracion**

Run: `dotnet ef migrations add TelegramActiveUserLinkIndexes --project src/Infrastructure --startup-project src/Api`

Revisar la migracion generada y reemplazar la creacion de indices por SQL Oracle equivalente a:

```sql
CREATE UNIQUE INDEX "UX_TELEGRAM_USER_LINKS_CHAT"
ON "TELEGRAM_USER_LINKS" (CASE WHEN "UNLINKED_AT" IS NULL THEN "TELEGRAM_CHAT_ID" END)
```

Repetir la expresion para `PERSON_ID` y `TELEGRAM_USER_ID`. `Down` debe restaurar los indices simples y documentar que solo es seguro si no existen duplicados historicos.

- [ ] **Step 4: Verificar GREEN y snapshot**

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~TelegramPersistenceTests" --no-restore --nologo`

Run: `dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Api`

Expected: pruebas PASS y ausencia de cambios pendientes.

### Task 3: Guiar al usuario no vinculado antes de llamar al agente

**Files:**
- Modify: `tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs`
- Modify: `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs`

**Interfaces:**
- Consumes: vinculo activo resuelto por `GetByTelegramUserIdAsync`.
- Produces: bienvenida con `/vincular` para `/start`, saludos y cualquier texto no vinculado, sin invocar `IAgentMessageDispatcher`.

- [ ] **Step 1: Escribir una prueba fallida para `/start` sin codigo**

Comprobar que `/start` sin payload produce una bienvenida que contiene `/vincular`, completa el update y no llama al agente.

- [ ] **Step 2: Ejecutar la prueba y confirmar RED**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~ProcessTelegramUpdateHandlerTests" --no-restore --nologo`

Expected: FAIL por el texto obsoleto que indica generar un codigo desde la aplicacion.

- [ ] **Step 3: Sustituir la respuesta no vinculada**

Usar una sola constante de bienvenida: explicar que la vinculacion se realiza una vez y pedir `Envía /vincular para comenzar`. No añadir deteccion semantica de saludos ni llamar al agente.

- [ ] **Step 4: Ejecutar las pruebas y confirmar GREEN**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~ProcessTelegramUpdateHandlerTests" --no-restore --nologo`

Expected: PASS.

### Task 4: Despertar el worker desde el webhook y conservar sondeo de respaldo

**Files:**
- Create: `src/Application/Telegram/Abstractions/ITelegramUpdateSignal.cs`
- Modify: `src/Application/Telegram/Updates/IngestTelegramUpdate.cs`
- Create: `src/Infrastructure/Telegram/Workers/InMemoryTelegramUpdateSignal.cs`
- Modify: `src/Infrastructure/Telegram/Workers/TelegramUpdateWorker.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Modify: `tests/Application.Tests/Telegram/TelegramApplicationHandlersTests.cs`
- Create: `tests/Infrastructure.Tests/Telegram/InMemoryTelegramUpdateSignalTests.cs`

**Interfaces:**
- Produces: `void Notify()` y `Task WaitAsync(TimeSpan fallbackInterval, CancellationToken cancellationToken)` en `ITelegramUpdateSignal`.
- Consumes: el handler llama `Notify()` solo despues de guardar un update aceptado; el worker llama `WaitAsync` cuando el pump no encuentra trabajo.

- [ ] **Step 1: Escribir pruebas fallidas de notificacion**

Comprobar que un update aceptado notifica una vez, un duplicado no notifica y la implementacion en memoria libera inmediatamente un waiter sin acumular notificaciones ilimitadas.

- [ ] **Step 2: Ejecutar las pruebas y confirmar RED**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~TelegramApplicationHandlersTests" --no-restore --nologo`

Expected: FAIL porque el puerto y la notificacion aun no existen.

- [ ] **Step 3: Implementar puerto, canal acotado y registro DI**

Implementar el adaptador con `Channel.CreateBounded<byte>(1)` y `BoundedChannelFullMode.DropWrite`. `WaitAsync` debe competir entre lectura y timeout cancelable, sin crear bucles de espera ni registrar contenido sensible.

- [ ] **Step 4: Reemplazar `Task.Delay` del worker por la senal**

Inyectar `ITelegramUpdateSignal` en el worker y esperar con `settings.WorkerPollInterval` unicamente cuando `RunOnceAsync` devuelve `false`.

- [ ] **Step 5: Ejecutar pruebas dirigidas y confirmar GREEN**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~Application.Tests.Telegram" --no-restore --nologo`

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~Infrastructure.Tests.Telegram" --no-restore --nologo`

Expected: PASS.

### Task 5: Configuracion, documentacion y verificacion

**Files:**
- Modify: `.env.example`
- Modify: `docs/integrations/telegram.md`

**Interfaces:**
- Consumes: `Telegram__WorkerPollMilliseconds`.
- Produces: valor recomendado `30000` y explicacion de que es respaldo, no latencia normal del webhook.

- [ ] **Step 1: Actualizar configuracion y documentacion**

Documentar `Telegram__WorkerPollMilliseconds=30000`, el flujo `/vincular`, la permanencia de la sesion vinculada y `Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Warning` para evitar consultas exitosas en logs.

- [ ] **Step 2: Ejecutar verificacion limitada**

Run: `dotnet test tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~Application.Tests.Telegram" --no-restore --nologo`

Run: `dotnet test tests/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~Infrastructure.Tests.Telegram" --no-restore --nologo`

Run: `dotnet build Veterinaria.sln --no-restore --nologo`

Run: `git diff --check`

Expected: todas las pruebas Telegram pasan, build con cero errores y diff sin errores de espacios.

