# Telegram worker concurrency (pool + orden por chat)

## Objetivo

Soportar ~20–50 chats concurrentes en un solo proceso backend, sin romper el orden de mensajes del mismo `TelegramChatId`.

## Enfoque

- `Telegram__WorkerConcurrency` (default `1`, rango 1–32): N slots en el mismo `TelegramUpdateWorker`.
- `ClaimNextAsync` solo toma Pending cuyo chat no tenga otro update activo (`Processing`/`Prepared` bajo lease).
- Reintentos de claim si hay carrera (`ExecuteUpdate` con `affected == 0`).
- Señal in-memory despierta a **todos** los waiters (broadcast).
- Rollout: desplegar con `1`, subir a `8` → `16–24` midiendo cola Pending y límites OpenRouter.

## Fuera de alcance

Réplicas de backend, cola Redis, cambios de LangGraph.
