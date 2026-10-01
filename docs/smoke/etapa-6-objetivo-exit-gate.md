# Smoke — puerta de salida Etapa 6 (objetivo de producto)

## Objetivo

Que un compañero, con **seed local** aplicado, demuestre el producto **sin** el portal Cliente JWT:

- **Web = solo staff**
- **Dueño = Telegram + teléfono + Gmail OTP** (autoservicio anónimo)
- Alta **sin password**
- OTP de cita **anónimo**
- Portal `/me` / `/mine` → **410 / ausente**, nunca 200

**Canal = Telegram. WhatsApp fuera de alcance.**

No reimplementa producto: reutiliza suites de Etapas 3–5 y 6.1–6.2. Gmail/Telegram reales **no** corren en CI (fakes).

## Precondiciones

1. Migraciones aplicadas.
2. Seeds del repo (no placeholders de tutorial):
   - `database/seeds/` / `apply_all.sql`
   - `database/test_seeds/README.md` — staff de prueba, p. ej. `admin` / `admin@veterinaria.com` / `Password123!`
3. Kickoff Etapa 6 leído: [`etapa-6-kickoff-gate.md`](etapa-6-kickoff-gate.md).

## Checklist mínimo (pasos 1–7)

| ID | Paso | Esperado | Cobertura |
|----|------|----------|-----------|
| S1 | Login **staff** (usuario del seed, p. ej. `admin@veterinaria.com`) | 200 + tokens | **Manual post-seed** (credenciales reales del entorno). Contrato Login/429: `RateLimitingTests` |
| S2 | Login con correo de **dueño** (Cliente / RegisterOwner) | Denegado: `Authentication.PlatformAccessDenied` o sin JWT | **Test CI** — `PlatformAccessDeniedAuthContractTests`, `RegisterOwnerLoginDeniedAcceptanceTests` |
| S3 | Lookup dueño por **teléfono** (anónimo) | 200 DTO mínimo / 404 negocio / 429 si se satura | **Test CI** — `ClientPhoneLookupHttpTests`, `ClientIdentificationLookupRateLimitHttpTests` |
| S4 | Request / confirm **Gmail** (ContactVerification Email) | 202 + proof; codes de catálogo; **sin SMTP real** | **Test CI** — suites Etapa 3 (filtro abajo) |
| S5 | **RegisterOwner** sin password | User Cliente, `PASSWORD_HASH` null, 0 accounts/creds | **Test CI** — `RegisterOwnerStage4AcceptanceTests`, `BotOwnerRegistrationHttpTests` |
| S6 | OTP cita `request-code` / `confirm-code` **sin JWT Cliente** | AllowAnonymous + rate limit; match teléfono de la cita | **Test CI** — `RequestAppointmentActionCodeCommandHandlerTests`, `ConfirmAppointmentActionCodeCommandHandlerTests`, `MyAppointmentsOtpSurfaceTests`, `AppointmentOtpRateLimitHttpTests`, `ClientOnlyPortalClosureHttpTests` (OTP anónimo) |
| S7 | Rutas portal (`/clients/me`, `/pets/mine`, `/appointments/mine` JWT, …) | **410** `ClientPortal.Gone` o ausentes; **no 200** | **Test CI** — `ClientOnlyPortalClosureTests`, `ClientsMeGoneTests`, `PetsMineGoneTests`, `MyAppointmentsCancelGoneTests`, `VaccinationsMineGoneTests`, `AccountStatementsMineGoneTests` |

### Solo manual (con motivo)

| ID | Motivo |
|----|--------|
| S1 (login staff 200 con seed) | Requiere Oracle + seed local; CI no monta esa BD. Usar credenciales de `database/test_seeds/README.md`, **nunca** `miusuario` / `user@example.com` inventado. |
| SMTP / Gmail real | Fuera de CI (Etapa 3: dispatcher fake). |
| Telegram real / webhook productivo | Fuera de CI; webhook inválido cubierto por rate-limit/tests de Telegram sin bot real. |

## Comandos `dotnet test` (CI / compañero)

Desde la raíz del repo. **Un solo filtro de cierre Etapa 6:**

```bash
dotnet test --filter "FullyQualifiedName~PlatformAccessDeniedAuthContractTests|FullyQualifiedName~RegisterOwnerLoginDeniedAcceptanceTests|FullyQualifiedName~RegisterOwnerStage4AcceptanceTests|FullyQualifiedName~BotOwnerRegistrationHttpTests|FullyQualifiedName~ContactEmailVerificationAcceptanceTests|FullyQualifiedName~ContactEmailVerificationRequestHandlerTests|FullyQualifiedName~ContactVerificationHttpTests|FullyQualifiedName~ConfirmAndConsumeContactEmailVerificationTests|FullyQualifiedName~ClientPhoneLookupHttpTests|FullyQualifiedName~ClientIdentificationLookupRateLimitHttpTests|FullyQualifiedName~RequestAppointmentActionCodeCommandHandlerTests|FullyQualifiedName~ConfirmAppointmentActionCodeCommandHandlerTests|FullyQualifiedName~AppointmentOtpRateLimitHttpTests|FullyQualifiedName~ClientOnlyPortalClosureTests|FullyQualifiedName~ClientsMeGoneTests|FullyQualifiedName~PetsMineGoneTests|FullyQualifiedName~MyAppointmentsCancelGoneTests|FullyQualifiedName~VaccinationsMineGoneTests|FullyQualifiedName~AccountStatementsMineGoneTests|FullyQualifiedName~RateLimitingTests|FullyQualifiedName~AnonymousChannelRateLimitCoverageTests|FullyQualifiedName~PiiInLogsTests|FullyQualifiedName~StableErrorCodeContractTests"
```

### Por bloque (si falla el filtro largo)

```bash
# S2 — dueño no entra a la web
dotnet test --filter "FullyQualifiedName~PlatformAccessDeniedAuthContractTests|FullyQualifiedName~RegisterOwnerLoginDeniedAcceptanceTests"

# S3 — lookup anónimo
dotnet test --filter "FullyQualifiedName~ClientPhoneLookupHttpTests|FullyQualifiedName~ClientIdentificationLookupRateLimitHttpTests"

# S4 — Gmail / ContactVerification (fake)
dotnet test --filter "FullyQualifiedName~ContactEmailVerificationAcceptanceTests|FullyQualifiedName~ContactEmailVerificationRequestHandlerTests|FullyQualifiedName~ContactVerificationHttpTests|FullyQualifiedName~ConfirmAndConsumeContactEmailVerificationTests"

# S5 — RegisterOwner sin password
dotnet test --filter "FullyQualifiedName~RegisterOwnerStage4AcceptanceTests|FullyQualifiedName~BotOwnerRegistrationHttpTests"

# S6 — OTP cita anónimo
dotnet test --filter "FullyQualifiedName~RequestAppointmentActionCodeCommandHandlerTests|FullyQualifiedName~ConfirmAppointmentActionCodeCommandHandlerTests|FullyQualifiedName~AppointmentOtpRateLimitHttpTests|FullyQualifiedName~ClientOnlyPortalClosureTests"

# S7 — portal muerto (410 / no 200)
dotnet test --filter "FullyQualifiedName~ClientOnlyPortalClosureTests|FullyQualifiedName~ClientsMeGoneTests|FullyQualifiedName~PetsMineGoneTests|FullyQualifiedName~MyAppointmentsCancelGoneTests|FullyQualifiedName~VaccinationsMineGoneTests|FullyQualifiedName~AccountStatementsMineGoneTests"

# Rate limit Anexo A + logs sin PII + codes
dotnet test --filter "FullyQualifiedName~RateLimitingTests|FullyQualifiedName~AnonymousChannelRateLimitCoverageTests|FullyQualifiedName~PiiInLogsTests|FullyQualifiedName~StableErrorCodeContractTests"
```

## Humo manual post-seed (opcional, 5–10 min)

Usar solo usuarios del seed (`database/test_seeds/README.md`).

| Paso | Acción | Esperado |
|------|--------|----------|
| M1 | `POST /api/auth/login` con `admin@veterinaria.com` / `Password123!` | 200 + access/refresh |
| M2 | Login con email de dueño de prueba (si existe account legacy) o tras RegisterOwner | 403 `Authentication.PlatformAccessDenied` o sin JWT |
| M3 | `GET /api/clients/by-phone/{phone}` con teléfono del seed | 200 o 404 |
| M4 | `GET /api/clients/me` | 410 `ClientPortal.Gone` |
| M5 | `POST .../appointments/mine/{id}/request-code` sin Authorization | 202/4xx de negocio (no 401 por falta de JWT) |

## Criterio de puerta

- [ ] Filtro de cierre Etapa 6 en **verde** (0 fallos).
- [ ] S1 manual OK en entorno con seed (o justificado si no hay BD local).
- [ ] Cero WhatsApp; canal dueño = Telegram + OTP anónimo/Gmail.
- [ ] Ningún paso propone conectar módulo Cliente en el front.

## Referencias

- ADR Etapa 6: [`docs/adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md`](../adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md)
- CONTEXT §0: [`docs/CONTEXT_REVISION_BACKEND.md`](../CONTEXT_REVISION_BACKEND.md)
- Gates previos: [`etapa-3-contact-email-exit-gate.md`](etapa-3-contact-email-exit-gate.md), [`etapa-4-register-owner-exit-gate.md`](etapa-4-register-owner-exit-gate.md), [`etapa-5-exit-gate.md`](etapa-5-exit-gate.md)
- Kickoff (no sustituye este doc): [`etapa-6-kickoff-gate.md`](etapa-6-kickoff-gate.md)
