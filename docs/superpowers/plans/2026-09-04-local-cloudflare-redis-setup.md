# Local Cloudflare and Redis Setup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Automatizar el Quick Tunnel de Telegram y alinear el agente Docker con Redis sin exponer secretos ni iniciar el backend.

**Architecture:** El backend tendrá un script PowerShell de operación local que administra el proceso `cloudflared`, actualiza una sola variable privada y registra el webhook. El agente seguirá usando su adaptador Redis existente; únicamente se alineará su `.env` privado con los valores que Docker Compose ya inyecta.

**Tech Stack:** PowerShell 5.1, Cloudflare Tunnel, Telegram Bot API, Docker Compose, Python 3.12, Redis 8.8.

## Global Constraints

- Trabajar directamente en la rama actual, sin worktree.
- No iniciar el backend.
- No imprimir ni versionar secretos o archivos `.env`.
- No modificar la arquitectura de puertos y adaptadores del agente.
- Ejecutar solamente verificaciones enfocadas.

---

### Task 1: Alinear Redis privado del agente

**Files:**
- Modify (ignored): `../Huellitas_ChatBot/.env`

**Interfaces:**
- Consumes: variables `HUELLITAS_REDIS_*` y `HUELLITAS_CHECKPOINT_*` de `Settings`.
- Produces: configuración local coherente con los servicios `redis` y `agent-api` de Compose.

- [ ] Establecer Redis habilitado, URL interna `redis://redis:6379`, base `0`, timeouts de cinco segundos, pool de 20 conexiones, cinco intentos, checkpoints Redis y TTL `10080`.
- [ ] Mantener usuario y contraseña vacíos para el Redis local sin autenticación.
- [ ] Ejecutar `docker compose config --quiet`.
- [ ] Comprobar dentro de Compose que el proveedor resuelto sea `redis` sin imprimir secretos.

### Task 2: Automatizar el Quick Tunnel y el webhook

**Files:**
- Create: `scripts/start-telegram-cloudflare-tunnel.ps1`
- Modify: `docs/integrations/telegram.md`

**Interfaces:**
- Consumes: `.env`, `cloudflared`, `Telegram__BotToken` y `Telegram__WebhookSecret`.
- Produces: URL pública activa, `Telegram__PublicWebhookUrl` actualizado y webhook registrado.

- [ ] Implementar lectura segura del `.env` y validación de variables requeridas.
- [ ] Iniciar `cloudflared tunnel --url http://localhost:5233` oculto y conservar su identificador de proceso.
- [ ] Esperar hasta 30 segundos por una URL `https://*.trycloudflare.com`.
- [ ] Actualizar solo `Telegram__PublicWebhookUrl` preservando las demás líneas del `.env`.
- [ ] Registrar `/api/integrations/telegram/webhook` con `setWebhook` sin mostrar secretos.
- [ ] Mantener el túnel vivo hasta `Ctrl+C` y detener el proceso hijo en `finally`.
- [ ] Documentar el orden exacto: túnel, backend manual y agente Docker.
- [ ] Validar sintaxis PowerShell y disponibilidad de `cloudflared`.

### Task 3: Verificación y entrega

**Files:**
- Verify: `.env` permanece ignorado.
- Verify: cambios versionados del backend.

**Interfaces:**
- Consumes: resultados de Tasks 1 y 2.
- Produces: configuración reproducible lista para prueba manual.

- [ ] Ejecutar validaciones enfocadas de Compose, settings y sintaxis PowerShell.
- [ ] Ejecutar `git diff --check` y comprobar que no haya secretos.
- [ ] Crear un commit Conventional Commit y dejar la rama lista para subir.
