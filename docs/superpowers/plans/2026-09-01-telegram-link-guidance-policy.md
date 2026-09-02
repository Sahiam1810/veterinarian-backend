# Telegram Link Guidance Policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminar la invitación repetitiva a `/vincular` de las respuestas generales de Telegram y conservarla solamente en la bienvenida o cuando la solicitud necesita identidad privada.

**Architecture:** .NET conserva los comandos y el estado de vinculación, pero deja de modificar cada respuesta producida por el agente. La política neutral `TelegramGuest` del agente decide condicionalmente cuándo orientar a vincular o crear una cuenta en la aplicación, sin ejecutar módulos ni recopilar credenciales.

**Tech Stack:** .NET 10, MediatR, xUnit, NSubstitute, Python 3.12, LangGraph, pytest, Ruff.

## Global Constraints

- Trabajar en `feature/telegram-link-guidance-policy` en ambos repositorios y sin worktree.
- No solicitar ni recibir contraseña, identificación o datos de registro mediante Telegram.
- No crear usuarios, clientes, conversaciones o participantes nuevos.
- No modificar JWT, OTP, persistencia, routing modular o contratos HTTP.
- `Telegram__GuestModeEnabled=false` debe conservar el modo estricto actual.
- Ejecutar solamente pruebas dirigidas de Telegram y la política invitada.
- Usar Conventional Commits con `fix: 🐛` para comportamiento y `docs: 📝` para documentación.

---

### Task 1: Entrega invitada sin sufijo incondicional en .NET

**Files:**
- Modify: `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs`
- Test: `tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs`

**Interfaces:**
- Consumes: `AgentMessageResult.Message` retornado por `IAgentMessageDispatcher`.
- Produces: entrega literal de una respuesta invitada no vacía y bienvenida estática para `/start` o respuesta vacía.

- [ ] **Step 1: Cambiar primero la prueba de una pregunta general invitada**

Renombrar `Unlinked_user_uses_isolated_guest_context_when_public_mode_is_enabled` a
`General_guest_response_is_delivered_without_automatic_linking_suffix` y reemplazar
su aserción final por:

```csharp
await fixture.Bot.Received(1).SendTextAsync(
    1001,
    "Cuidados generales",
    default);
```

Esta prueba falla si .NET vuelve a concatenar cualquier texto, incluido `/vincular`.

- [ ] **Step 2: Ejecutar la prueba y confirmar RED**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~General_guest_response_is_delivered_without_automatic_linking_suffix"
```

Expected: FAIL porque el texto real contiene `GuestLinkingHint`.

- [ ] **Step 3: Proteger la bienvenida y el modo estricto con pruebas existentes**

Conservar las pruebas que demuestran que:

```csharp
Assert.Contains("/vincular", deliveredStartReply, StringComparison.OrdinalIgnoreCase);
```

aplica a `/start` invitado y que, cuando `GuestModeEnabled` es falso, no se llama
al agente y se entrega `LinkingRequiredReply`.

- [ ] **Step 4: Implementar el cambio mínimo**

Eliminar `GuestLinkingHint` y dejar el cierre de `ProcessGuestMessageAsync` así:

```csharp
var response = string.IsNullOrWhiteSpace(result.Message)
    ? GuestStartReply
    : result.Message;
await DeliverAsync(update, response, cancellationToken);
```

Actualizar `GuestStartReply` para explicar en una sola bienvenida:

```csharp
private const string GuestStartReply =
    "¡Hola! Puedes hacer preguntas veterinarias generales como invitado. " +
    "Para consultar tus mascotas o realizar operaciones, envía /vincular; " +
    "si aún no tienes una cuenta, deberás crearla de forma segura en la aplicación.";
```

- [ ] **Step 5: Ejecutar el corte Telegram y confirmar GREEN**

Run:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~Telegram"
```

Expected: todas las pruebas Telegram de Application pasan.

- [ ] **Step 6: Commit funcional del backend**

```powershell
git add src/Application/Telegram/Processing/ProcessTelegramUpdate.cs tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs
git commit -m "fix: 🐛 avoid repeated Telegram linking hints"
```

---

### Task 2: Política condicional para solicitudes privadas en el agente

**Files (repository `Huellitas_ChatBot`):**
- Modify: `src/app/orchestration/guest_access.py`
- Test: `tests/unit/orchestration/test_message_processor.py`
- Test: `tests/unit/orchestration/test_main_graph.py`

**Interfaces:**
- Consumes: rol exacto `TelegramGuest` y `MessageCommand` existente.
- Produces: `GUEST_SYSTEM_PROMPT` que permite respuestas generales sin llamada a vincular y exige orientación para datos u operaciones privadas.

- [ ] **Step 1: Endurecer la prueba del prompt antes de modificarlo**

En `test_guest_uses_policy_prompt_and_cannot_publish_global_knowledge`, conservar
la comprobación del primer mensaje `SYSTEM` y agregar:

```python
guest_prompt = request.messages[0].content
assert "/vincular" in guest_prompt
assert "do not append" in guest_prompt.lower()
assert "create an account securely in the application" in guest_prompt.lower()
```

La mutación que esta prueba captura es volver a convertir la invitación en un
sufijo general o sugerir registro dentro del chat.

- [ ] **Step 2: Ejecutar la prueba y confirmar RED**

Run:

```powershell
uv run pytest tests/unit/orchestration/test_message_processor.py::test_guest_uses_policy_prompt_and_cannot_publish_global_knowledge -q
```

Expected: FAIL porque el prompt actual no contiene las dos reglas nuevas.

- [ ] **Step 3: Implementar la instrucción neutral y condicional**

Reemplazar `GUEST_SYSTEM_PROMPT` por:

```python
GUEST_SYSTEM_PROMPT = (
    "You are serving an unlinked Telegram guest. Answer general veterinary and "
    "public clinic questions normally. Do not append a linking reminder to a "
    "general answer. Never claim access to pets, appointments, vaccines, medical "
    "records, or account data, and never confirm a business operation. Only when "
    "the current request requires personalized data or an operation, instruct the "
    "user to send /vincular. If the user says they have no Huellitas account, explain "
    "that they must create an account securely in the application; never request a "
    "password, identification number, or registration data in Telegram."
)
```

- [ ] **Step 4: Ejecutar las regresiones invitadas y confirmar GREEN**

Run:

```powershell
uv run pytest tests/unit/orchestration/test_message_processor.py::test_guest_uses_policy_prompt_and_cannot_publish_global_knowledge tests/unit/orchestration/test_main_graph.py::test_telegram_guest_never_calls_router_or_module_executor -q
uv run ruff check src/app/orchestration/guest_access.py tests/unit/orchestration/test_message_processor.py
```

Expected: 2 pruebas pasan y Ruff no reporta errores.

- [ ] **Step 5: Commit funcional del agente**

```powershell
git add src/app/orchestration/guest_access.py tests/unit/orchestration/test_message_processor.py
git commit -m "fix: 🐛 make guest linking guidance conditional"
```

---

### Task 3: Documentación y verificación coordinada

**Files:**
- Modify backend: `docs/integrations/telegram.md`
- Modify agent: `docs/Distribución de la arquitectura del servicio de automatización.md`

**Interfaces:**
- Consumes: comportamiento verificado de Tasks 1 y 2.
- Produces: guía operativa que distingue bienvenida, pregunta general, solicitud privada y registro externo.

- [ ] **Step 1: Actualizar la guía del canal**

Documentar en `docs/integrations/telegram.md`:

```text
/start explica el modo invitado y /vincular una sola vez.
Las respuestas generales no reciben un sufijo automático.
Las solicitudes privadas orientan a /vincular desde la política del agente.
La creación de cuenta ocurre fuera de Telegram y nunca solicita contraseñas en el chat.
```

- [ ] **Step 2: Actualizar el estado de arquitectura del agente**

En la sección `TelegramGuest`, registrar que la orientación es condicional y
que la compuerta sigue impidiendo router, módulos, RAG directo y publicación global.

- [ ] **Step 3: Ejecutar verificación final dirigida**

Backend:

```powershell
dotnet test tests/Application.Tests/Application.Tests.csproj --no-restore --filter "FullyQualifiedName~Telegram"
dotnet build veterinarian_backend.slnx --no-restore
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Api --no-build
git diff --check develop...HEAD
```

Agent:

```powershell
uv run pytest tests/unit/orchestration/test_main_graph.py tests/unit/orchestration/test_message_processor.py -q
uv run ruff check src/app/orchestration/guest_access.py src/app/orchestration/main_graph.py src/app/orchestration/message_processor.py tests/unit/orchestration/test_main_graph.py tests/unit/orchestration/test_message_processor.py
uv run python -m compileall -q src
git diff --check develop...HEAD
```

Expected: pruebas y builds pasan, no existe cambio de modelo EF y ambos árboles
están limpios salvo los documentos pendientes de commit.

- [ ] **Step 4: Commit documental del backend**

```powershell
git add docs/integrations/telegram.md docs/superpowers/plans/2026-09-01-telegram-link-guidance-policy.md
git commit -m "docs: 📝 document conditional Telegram linking guidance"
```

- [ ] **Step 5: Commit documental del agente**

```powershell
git add "docs/Distribución de la arquitectura del servicio de automatización.md"
git commit -m "docs: 📝 document conditional guest guidance"
```

---

## Out of Scope

- Registrar cuentas dentro de Telegram.
- Configurar una URL de frontend que todavía no existe.
- Implementar o registrar `pet_profile`.
- Consultar, crear, actualizar o eliminar mascotas.
- Cambiar el contrato de respuesta del agente.
