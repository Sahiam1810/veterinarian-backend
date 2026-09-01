# Telegram Guest Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Habilitar consultas generales publicas en Telegram sin debilitar OTP, JWT ni el aislamiento de modulos privados.

**Architecture:** .NET emite una identidad invitada firmada y deterministica sin persistirla en Oracle, mientras el agente fuerza `TelegramGuest` a la ruta general. La vinculacion existente continua usando identidades y conversaciones reales.

**Tech Stack:** .NET 10, EF Core/Oracle, MediatR, JWT RS256, Python 3.12, FastAPI, LangGraph, pytest, xUnit.

## Global Constraints

- Usar `feature/telegram-guest-mode` en ambos repositorios y ningun worktree.
- Conservar `identity_mismatch` y todos los claims JWT requeridos.
- No crear usuarios, clientes, participantes ni conversaciones invitadas en Oracle.
- No permitir modulos ni publicacion global para `TelegramGuest`.
- No registrar JWT, mensaje, respuesta, IDs de Telegram ni UUID invitados.
- Ejecutar solamente pruebas dirigidas de Telegram, JWT, processor y grafo durante el desarrollo.

---

### Task 1: Identidad invitada firmada en .NET

**Files:**
- Modify: `src/Application/Telegram/Abstractions/IAgentDelegatedIdentityProvider.cs`
- Modify: `src/Infrastructure/Telegram/Security/AgentDelegatedIdentityProvider.cs`
- Modify: `src/Infrastructure/Security/Tokens/JwtTokenIssuer.cs`
- Modify: `tests/Infrastructure.Tests/Telegram/AgentDelegatedIdentityProviderTests.cs`

**Interfaces:**
- Produces: `AgentDelegatedIdentity GetGuest(long telegramUserId)` con rol fijo `TelegramGuest` y UUID deterministico.

- [ ] Escribir pruebas que exijan claims completos, estabilidad por usuario y separacion entre usuarios; ejecutarlas y confirmar RED.
- [ ] Implementar derivacion SHA-256 con etiquetas separadas para account/person/role, emitir RS256 con duracion delegada y confirmar GREEN.
- [ ] Commit: `feat: ✨ issue isolated Telegram guest identities`.

### Task 2: Despacho invitado sin persistencia Oracle

**Files:**
- Modify: `src/Application/Telegram/Abstractions/ITelegramRuntimeSettings.cs`
- Modify: `src/Infrastructure/Telegram/Configuration/TelegramOptions.cs`
- Modify: `src/Infrastructure/Telegram/Configuration/TelegramOptionsValidator.cs`
- Modify: `src/Infrastructure/Telegram/Configuration/ConfiguredTelegramRuntimeSettings.cs`
- Modify: `src/Infrastructure/DependencyInjection.cs`
- Modify: `src/Application/Telegram/Processing/ProcessTelegramUpdate.cs`
- Modify: `tests/Application.Tests/Telegram/ProcessTelegramUpdateHandlerTests.cs`
- Modify: `tests/Infrastructure.Tests/Telegram/TelegramOptionsValidatorTests.cs`

**Interfaces:**
- Produces: `GuestModeEnabled`; contexto invitado UUID deterministico por chat; `/start` estatico; despacho general con `PetId=null`, `Role=TelegramGuest` y `PublishAsGlobalKnowledge=false`.

- [ ] Escribir pruebas para modo desactivado, saludo invitado, `/start`, no persistencia y flujo vinculado intacto; confirmar RED.
- [ ] Implementar el branching minimo despues del linking service y antes de la respuesta estricta; confirmar GREEN.
- [ ] Commit: `feat: ✨ allow public Telegram guest conversations`.

### Task 3: Compuerta invitada en LangGraph

**Files (repositorio `Huellitas_ChatBot`):**
- Modify: `src/app/orchestration/main_graph.py`
- Modify: `src/app/orchestration/message_processor.py`
- Modify: `tests/unit/orchestration/test_main_graph.py`
- Modify: `tests/unit/orchestration/test_message_processor.py`

**Interfaces:**
- Consumes: rol exacto `TelegramGuest` en `MessageCommand.roles`.
- Produces: fallback `guest_general_only`, `allow_direct=false`, prompt seguro y publicacion global deshabilitada.

- [ ] Escribir prueba con registro modular falso que demuestre que invitado no llama router/ejecutor; confirmar RED.
- [ ] Escribir prueba del processor que exija system prompt invitado y `knowledge_published=false`; confirmar RED.
- [ ] Implementar constantes/politicas neutrales en orchestration sin importar Telegram ni Infrastructure; confirmar GREEN.
- [ ] Commit: `feat: ✨ restrict guest identities to general routing`.

### Task 4: Configuracion, documentacion y verificacion coordinada

**Files:**
- Modify backend: `.env.example`, `.env`, `docs/integrations/telegram.md`.
- Modify agent: `docs/Distribución de la arquitectura del servicio de automatización.md`, `docs/jwt-authentication.md`.

- [ ] Documentar `Telegram__GuestModeEnabled`, identidad efimera, aislamiento y comportamiento de `/vincular`.
- [ ] Ejecutar 57 pruebas Telegram del backend, pruebas dirigidas del agente, builds y `git diff --check` en ambos repositorios.
- [ ] Confirmar que no hay migracion EF y que los arboles quedan limpios.
- [ ] Commit backend: `docs: 📝 document Telegram guest mode`.
- [ ] Commit agent: `docs: 📝 document guest access policy`.
