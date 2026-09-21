# Catálogo de `code` — API problem+json

**Propósito:** el front **staff** y el bot **Telegram** traducen UX solo por `code` (no por `title` / `message` / `Description`).  
**OTP en web:** el frontend staff **no** implementa OTP. Canal chatbot = **Telegram** únicamente (WhatsApp fuera de alcance).

**Contrato del refactor:** [`clientes-usuarios-api-v2.md`](clientes-usuarios-api-v2.md).  
**Doc de etapa:** [`docs/adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md`](../adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md).

## Familias (para quién / qué no es)

| Familia | Para quién | Qué no es |
|---------|------------|-----------|
| `Authentication.*` | Front staff (login / JWT / plataforma) | No es “login de dueño” ni OTP; **ya no** se usan para conflictos de alta de cliente |
| `ContactVerification.*` | Bot Telegram (Gmail / proof / Claim) | No es OTP de cita |
| Alta cliente (`OwnerRegistration.*`, `Clients.*`) | Staff (`POST /api/clients`) y bot (`/api/owners/bot`) | No pide password al dueño |
| `Telegram.*` (vínculo / claim) | Bot Telegram | No es ContactVerification Gmail ni OTP de cita |
| `ClientPortal.Gone` | Quien aún llame rutas `/mine` u otras de portal | El portal Cliente no vuelve |
| `AppointmentAction.*` (OTP de cita) | Bot Telegram | No es Gmail / ContactVerification |
| `RateLimit.Exceeded` | Todos los anónimos / policies RL | No es 401 |

## Dónde vive la fuente de verdad

| Área | Archivo en código |
|------|-------------------|
| Auth / plataforma | [`AuthenticationErrors.cs`](../../src/Application/Security/Errors/AuthenticationErrors.cs) |
| Rate limit | [`RateLimitErrors.cs`](../../src/Application/Security/Errors/RateLimitErrors.cs) |
| Portal Cliente retirado | [`ClientPortalErrors.cs`](../../src/Application/Security/Errors/ClientPortalErrors.cs) |
| Contact verification (Gmail) | [`ContactVerificationErrors.cs`](../../src/Application/ContactVerification/Errors/ContactVerificationErrors.cs) |
| RegisterOwner / alta bot | [`OwnerRegistrationErrors.cs`](../../src/Application/Owners/Errors/OwnerRegistrationErrors.cs) |
| Cliente (validación / conflicto) | [`ClientErrorCodes.cs`](../../src/Application/Clients/Errors/ClientErrorCodes.cs) |
| OTP acción de cita | [`AppointmentActionErrors.cs`](../../src/Application/Appointments/Errors/AppointmentActionErrors.cs) |

Nuevo `code` → constante/`Error` en el `*Errors.cs` correspondiente **y** una fila aquí.

## Forma de respuesta (problem+json)

```json
{
  "type": "https://httpstatuses.com/401",
  "title": "Unauthorized",
  "status": 401,
  "code": "Authentication.InvalidCredentials"
}
```

Algunos endpoints legacy aún usan `ApiErrorResponse` sin `code`. No inventar `code` ad hoc en el controller: migrar vía excepción tipada + `GlobalExceptionHandler`.

---

## Authentication.* — staff login / JWT

**Para quién:** front staff. **Qué no es:** login de dueño, Gmail, OTP de cita, ni conflictos de cédula/correo de **cliente**.  
**Fuente:** [`AuthenticationErrors.cs`](../../src/Application/Security/Errors/AuthenticationErrors.cs)

| code | HTTP | Cuándo |
|------|------|--------|
| `Authentication.InvalidCredentials` | 401 | Login fallido (credenciales) |
| `Authentication.InvalidRefreshToken` | 401 | Refresh inválido/vencido |
| `Authentication.Unauthorized` | 401 | JWT ausente/inválido (challenge) |
| `Authentication.Forbidden` | 403 | Autenticado sin permiso de recurso |
| `Authentication.PlatformAccessDenied` | 403 | Rol no admitido en esa plataforma/front |
| `Authentication.InvalidRegistrationData` | 400 | Datos de registro de personal inválidos |

**Retirados como códigos de alta de cliente** (usar `Clients.*`):

| code (legado) | Reemplazo |
|---------------|-----------|
| `Authentication.UserAlreadyExists` | `Clients.EmailAlreadyInUse` |
| `Authentication.IdentificationNumberAlreadyExists` | `Clients.IdentificationAlreadyInUse` |

---

## ContactVerification.* — Gmail / proof (Telegram)

**Para quién:** bot Telegram (verificación de correo; Claim por cédula). **Qué no es:** OTP de cita (`AppointmentAction.*`).  
**Fuente:** [`ContactVerificationErrors.cs`](../../src/Application/ContactVerification/Errors/ContactVerificationErrors.cs)

| code | HTTP | Cuándo |
|------|------|--------|
| `ContactVerification.InvalidCode` | 400 | OTP Gmail incorrecto |
| `ContactVerification.Expired` | 409 | Sesión/OTP Gmail vencido |
| `ContactVerification.Blocked` | 409 | Intentos agotados / sesión bloqueada |
| `ContactVerification.ResendTooSoon` | 409 | Reenvío antes del intervalo |
| `ContactVerification.SessionNotFound` | 404 | Sesión de verificación inexistente |
| `ContactVerification.ProofInvalid` | 400 | Proof inválido |
| `ContactVerification.ProofAlreadyConsumed` | 409 | Proof ya consumido |
| `ContactVerification.ProofExpired` | 409 | Proof vencido |
| `ContactVerification.ChannelNotSupported` | 400 | Canal no soportado |
| `ContactVerification.PurposeInvalid` | 400 | Purpose inválido (p. ej. `Claim` en `email/request`; Claim va por `request-claim-by-identification`) |
| `ContactVerification.EmailInvalid` | 400 | Formato de correo inválido |
| `ContactVerification.DeliveryFailed` | 409 | Fallo al enviar (SMTP); sin detalle de proveedor |
| `ContactVerification.NotImplemented` | 501 | Capacidad aún no implementada |

---

## Alta cliente — Clients.* / OwnerRegistration.*

**Para quién:** staff (`POST /api/clients`) y bot (`POST /api/owners/bot`). **Qué no es:** pedir password al dueño; no es OTP de cita.  
**Fuentes:** [`ClientErrorCodes.cs`](../../src/Application/Clients/Errors/ClientErrorCodes.cs), [`OwnerRegistrationErrors.cs`](../../src/Application/Owners/Errors/OwnerRegistrationErrors.cs)

| code | HTTP | Cuándo |
|------|------|--------|
| `Clients.PhoneRequired` | 400 | Teléfono obligatorio |
| `Clients.PhoneInvalidFormat` | 400 | Formato de teléfono inválido |
| `Clients.PhoneAlreadyInUse` | 409 | Teléfono ya usado |
| `Clients.EmailAlreadyInUse` | 409 | Correo ya usado por otro cliente |
| `Clients.IdentificationAlreadyInUse` | 409 | Cédula ya registrada |
| `OwnerRegistration.ProofRequired` | 400 | Falta proof de ContactVerification (alta staff con proofs exigidos) |
| `OwnerRegistration.ProofEmailMismatch` | 400 | Proof no corresponde al email |
| `OwnerRegistration.ProofPurposeInvalid` | 400 | Purpose del proof incorrecto |

`POST /api/owners/bot` responde **201** `{ clientId }` (sin `userId`).

---

## Telegram — vínculo bot-link / claim

**Para quién:** bot Telegram. **Qué no es:** Gmail OTP ni OTP de cita.

| code | HTTP | Cuándo |
|------|------|--------|
| `Telegram.ClientLinkRequiresProof` | 403 | `bot-link` con `{ clientId }` fuera de la ventana de registro reciente o sin derecho a vínculo directo; usar Claim OTP |
| (409 identidad) | 409 | Telegram ya vinculado a otro cliente / proof ya usado o vencido (vía `ContactVerification.*` o conflicto de vínculo) |

**Retirados (D4/U3 — registro web / link-codes):**

| code | Notas |
|------|--------|
| `Telegram.Registration.InvalidOrExpired` | Enlace web de registro; flujo eliminado |
| `Telegram.Registration.IdentityConflict` | Idem |

---

## ClientPortal.Gone — portal retirado

**Para quién:** cualquier cliente que aún llame rutas de portal (`/mine`, stubs 410, etc.). **Qué no es:** un 404 temporal; el portal **no** vuelve.  
**Fuente:** [`ClientPortalErrors.cs`](../../src/Application/Security/Errors/ClientPortalErrors.cs)

| code | HTTP | Cuándo |
|------|------|--------|
| `ClientPortal.Gone` | 410 | Ruta de portal Cliente retirada; usar chatbot/staff |

No documentar `/clients/me` 200 como producto.

---

## AppointmentAction.* — OTP de cita (Telegram)

**Para quién:** bot Telegram (cancelar/reagendar con SMS OTP). **Qué no es:** ContactVerification Gmail; la web staff no consume estos codes.  
**Fuente:** [`AppointmentActionErrors.cs`](../../src/Application/Appointments/Errors/AppointmentActionErrors.cs)

| code | HTTP | Cuándo |
|------|------|--------|
| `AppointmentAction.PhoneMismatch` | 401 | Teléfono ≠ requester de la cita / sesión |
| `AppointmentAction.InvalidCode` | 401 | OTP de cita incorrecto |
| `AppointmentAction.Expired` | 409 | OTP de cita vencido |
| `AppointmentAction.AttemptsExhausted` | 409 | Intentos agotados; pedir código nuevo |
| `AppointmentAction.ResendTooSoon` | 409 | Reenvío antes del intervalo |
| `AppointmentAction.DeliveryFailed` | 409 | Fallo al despachar SMS OTP |
| `AppointmentAction.SessionNotFound` | 404 | Sin verificación activa para la cita/acción |
| `AppointmentAction.InvalidAction` | 400 | Acción distinta de cancel/reschedule |

---

## RateLimit.Exceeded — anónimos / policies

**Para quién:** cualquier caller que dispare una policy de rate limit. **Qué no es:** 401 (credenciales).  
**Fuente:** [`RateLimitErrors.cs`](../../src/Application/Security/Errors/RateLimitErrors.cs) (emitido desde `RateLimitingExtensions.OnRejected`)

| code | HTTP | Cuándo |
|------|------|--------|
| `RateLimit.Exceeded` | 429 | Demasiadas solicitudes en la ventana de la policy |

---

## Reglas

1. Staff UI y Telegram: switch/i18n por `code` únicamente.
2. No loguear `Description` / body con datos sensibles; en logs usar `code` + ids.
3. Rate limit 429: siempre `RateLimit.Exceeded` (no inventar un code distinto por ruta).
4. Distinguir **siempre** `ContactVerification.*` (Gmail) de `AppointmentAction.*` (OTP cita).
5. Conflictos de cliente: preferir `Clients.*`; no mapear el alta de dueño a `Authentication.UserAlreadyExists`.
6. No añadir WhatsApp a este catálogo.
