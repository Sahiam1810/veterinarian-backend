# ADR: Etapa 6 — rate limit, logs seguros y catálogo de `code` (kickoff)

- Estado: Aceptada (kickoff)
- Fecha: 2026-09-07
- Decisores: Equipo de desarrollo

## Contexto

Etapa 5 cerró el portal JWT Cliente. Quedan superficies **anónimas** y de **bot/Telegram** expuestas a fuerza bruta y a fuga de PII en logs. Si 6.1–6.5 arrancan sin inventario ni convenciones, cinco personas inventan `code` distintos, pelean `Program.cs` / `RateLimitingExtensions.cs` y loguean teléfono/OTP en claro.

Este kickoff **no** implementa producto. Solo fija inventario, dueños de archivo y contratos.

**Canal de dueño = Telegram. WhatsApp está fuera de alcance** (no stub, no “pendiente” de esta etapa, no DTO).

No se reabre portal Cliente ni OTP en la web staff.

## Decisiones

### 1. Inventario de rutas que deben tener rate limit (Anexo A)

Toda ruta del Anexo A debe quedar cubierta por una policy en `RateLimitPolicies` + registro en `AddApiRateLimiting` + `[EnableRateLimiting(...)]` en el endpoint (o atributo de controller si aplica). Partición: por `sub` si hay usuario autenticado, por IP si es anónimo (patrón vigente).

6.1 implementa/ajusta límites; este kickoff solo lista. Varias policies ya existen — 6.1 verifica cobertura y no deja huecos del anexo.

### 2. Convención de logs (sin secretos ni PII en claro)

Prohibido en logs (ILogger, excepciones serializadas a log, telemetría):

- Teléfono (raw o normalizado)
- Cédula / número de identificación
- Código OTP en claro
- Proof / completion token / link code en claro
- Cuerpo o asunto de correo completo
- Password / refresh token / pepper

Permitido:

- Ids: `clientId`, `userId`, `appointmentId`, `sessionId` (GUID)
- Resultado: `Succeeded` / fallido + **`code`** estable del catálogo
- Canal genérico: `Email`, `Telegram` (no el destino)

Cualquier logger nuevo en 6.2+ debe cumplir esta regla. No se “depuran” dumps de request body en producción.

### 3. Catálogo de `code` (front staff traduce solo por `code`)

1. Doc canónico: [`docs/contracts/api-error-codes-catalog.md`](../contracts/api-error-codes-catalog.md).
2. Fuente en código: clases `*Errors.cs` bajo `Application/**/Errors/` (punteros en ese doc).
3. Respuestas tipadas usan `application/problem+json` con campo `code` cuando el pipeline ya lo soporta (auth, contact verification, owner bot, `ClientPortal.Gone`, etc.).
4. **El front staff traduce solo por `code`.** No hay OTP en la UI web del personal; OTP de cita y Gmail viven en chatbot/flujos anónimos, no en pantalla staff.

Prohibido inventar strings de `code` sueltos en controllers. Nuevo code → entrada en `*Errors.cs` + línea en el catálogo (tarea 6.x de codes, no este kickoff).

### 4. Checklist de humo post-seed

Ver [`docs/smoke/etapa-6-kickoff-gate.md`](../smoke/etapa-6-kickoff-gate.md) § humo. Usuarios y cuentas del **seed real** del repo (`database/seeds`, `database/test_seeds/README.md`, `docs/SUPERADMIN_PROVISIONING.md`). **Prohibido** copiar placeholders de tutorial (`miusuario`, `nombretabla`, `user@example.com` inventado sin estar en seed).

### 5. Dueños de archivo (para no pelear el mismo PR)

| Rol | Tarea típica | Archivos que **solo** esa persona edita en su slice |
|-----|--------------|------------------------------------------------------|
| **RL** | 6.1 rate limit | `Api/Common/Security/RateLimitPolicies.cs`, `Api/Extensions/RateLimitingExtensions.cs`, `Api/Configuration/RateLimitOptions*.cs`, sección `RateLimiting` de `appsettings*.json`, **líneas** de `Program.cs` que registran/usan rate limiting, atributos `[EnableRateLimiting]` en controllers del Anexo A |
| **LOG** | 6.2 logs seguros | filtros/enrichers de logging, helpers de scrub; **no** tocar RateLimiting ni Program rate limit |
| **CODE** | catálogo / alineación codes | `docs/contracts/api-error-codes-catalog.md`, `Application/**/Errors/*Errors.cs`, wiring en `GlobalExceptionHandler` solo si el code nuevo lo exige |
| **CTX** | CONTEXT | `docs/CONTEXT_REVISION_BACKEND.md` (secciones Etapa 6 / rate limit / logs) |
| **SMOKE** | humo post-seed | `docs/smoke/etapa-6-*.md` (y scripts de smoke si se agregan después; no en kickoff) |

`Program.cs`: el dueño **RL** es el único que toca el bloque rate limiting. Otros slices no reordenan el pipeline “por si acaso”.

Las cinco tareas **no se esperan entre sí** tras mergear este kickoff.

## Anexo A — rutas a cubrir (copiable; 6.1 implementa)

Inventario verificado en código (2026-09-07). Policy actual entre paréntesis si ya existe.

```text
# Login / refresh staff
POST   /api/auth/login                                          (Login)
POST   /api/auth/refresh                                        (Refresh)

# Lookup dueño (anónimo)
GET    /api/clients/by-phone/{phone}                            (ClientPhoneLookup)
GET    /api/clients/by-identification/{identificationNumber}    (ClientIdentificationLookup)

# Gmail request / confirm (ContactVerification Email)
POST   /api/contact-verification/email/request                  (ContactEmailRequest)
POST   /api/contact-verification/email/confirm                  (ContactEmailConfirm)

# RegisterOwner bot / Telegram
POST   /api/owners/bot                                          (BotOwnerRegistration)
GET    /telegram/registration/complete                          (TelegramRegistration)
POST   /telegram/registration/complete                          (TelegramRegistration)

# Webhook Telegram
POST   /api/integrations/telegram/webhook                       (TelegramWebhook)

# OTP de cita (anónimo; no es Gmail)
POST   /api/appointments/mine/{id}/request-code                 (AppointmentOtpRequest)
POST   /api/appointments/mine/{id}/confirm-code                 (AppointmentOtpConfirm)
```

Notas:

- OTP cita ≠ OTP Gmail (ADR límites OTP + Etapa 5).
- `POST /api/clients/register-owner` (staff autenticado) no está en el anexo mínimo; si 6.1 lo añade, documentarlo en la PR sin pelear el anexo.
- `POST /api/integrations/telegram/link-codes` no está en el anexo de kickoff; no expandir alcance sin actualizar este ADR.

## Mapa orientativo 6.1–6.5 (post-kickoff)

| Tarea | Enfoque | No hace |
|-------|---------|---------|
| **6.1** | Cubrir Anexo A (policies + límites + tests 429) | No inventa codes; no WhatsApp |
| **6.2** | Cumplir convención de logs (§2) | No toca RateLimitingExtensions |
| **6.3** | Completar/alinear catálogo `code` | No reabre portal Cliente |
| **6.4** | CONTEXT / docs operativos | No cambia Program.cs rate limit |
| **6.5** | Humo post-seed con usuarios reales del seed | No usa placeholders de tutorial |

## Alternativas descartadas

- Un solo PR gigante 6.1–6.5: pelea de archivos y review imposible.
- Loguear request body “solo en Development”: acaba en prod o en capturas.
- Codes distintos por endpoint sin `*Errors.cs`: el front staff no puede traducir.
- Mencionar WhatsApp como pendiente de Etapa 6: fuera de alcance.

## Consecuencias

- Kickoff mergea solo docs (este ADR, catálogo, smoke gate, enlace CONTEXT).
- 6.1–6.5 implementan sin reinterpretar el anexo ni pelear dueños de archivo.

## Criterios de aceptación del kickoff

- [x] ADR mergeado y enlazado desde CONTEXT.
- [x] Lista de rutas anónimas/bot con rate limit (Anexo A).
- [x] Convención de logs escrita.
- [x] Catálogo de codes + punteros a `*Errors.cs`.
- [x] Checklist humo post-seed (usuarios reales del seed).
- [x] Frase: canal = Telegram; WhatsApp fuera de alcance.
- [x] Dueños de archivo anotados (RL / LOG / CODE / CTX / SMOKE).
- [x] Cero código de producto en este cambio.

## Criterios de revisión (PRs 6.1–6.5)

- [ ] ¿Dos slices editan `RateLimitingExtensions` o el bloque rate limit de `Program.cs`?
- [ ] ¿Loguea teléfono, cédula, OTP, proof o cuerpo de correo?
- [ ] ¿Inventa `code` fuera de `*Errors.cs` / catálogo?
- [ ] ¿Declara WhatsApp o reabre portal Cliente / OTP web staff?
- [ ] ¿Humo usa placeholders de tutorial en vez del seed del repo?
