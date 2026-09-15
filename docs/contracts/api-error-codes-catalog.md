# Catálogo de `code` — API problem+json

**Propósito:** el front **staff** y el bot **Telegram** traducen UX solo por `code` (no por `title` / `message` / `Description`).  
**OTP en web:** el frontend staff **no** implementa OTP. Canal chatbot = **Telegram** únicamente (WhatsApp fuera de alcance).

**Doc de etapa:** [`docs/adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md`](../adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md).  
**Tarea:** 6.3 — alinear catálogo + codes estables mínimos en `*Errors.cs`.

## Familias (para quién / qué no es)

| Familia | Para quién | Qué no es |
|---------|------------|-----------|
| `Authentication.*` | Front staff (login / JWT / plataforma) | No es “login de dueño” ni OTP |
| `ContactVerification.*` | Bot Telegram (Gmail / proof) | No es OTP de cita |
| Alta dueño (`OwnerRegistration.*`, `Clients.Phone*`, aliases Auth en RegisterOwner) | Staff y bot | No pide password |
| `Telegram.Registration.*` | Bot Telegram (enlace de alta) | No es ContactVerification Gmail ni OTP de cita |
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
| RegisterOwner | [`OwnerRegistrationErrors.cs`](../../src/Application/Owners/Errors/OwnerRegistrationErrors.cs) |
| Teléfono cliente (validación / conflicto) | [`ClientErrorCodes.cs`](../../src/Application/Clients/Errors/ClientErrorCodes.cs) |
| Telegram registration | [`TelegramRegistrationErrors.cs`](../../src/Application/Telegram/Registration/TelegramRegistrationErrors.cs) |
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

**Para quién:** front staff. **Qué no es:** login de dueño, Gmail, OTP de cita.  
**Fuente:** [`AuthenticationErrors.cs`](../../src/Application/Security/Errors/AuthenticationErrors.cs)

| code | HTTP | Cuándo |
|------|------|--------|
| `Authentication.InvalidCredentials` | 401 | Login fallido (credenciales) |
| `Authentication.InvalidRefreshToken` | 401 | Refresh inválido/vencido |
| `Authentication.Unauthorized` | 401 | JWT ausente/inválido (challenge) |
| `Authentication.Forbidden` | 403 | Autenticado sin permiso de recurso |
| `Authentication.PlatformAccessDenied` | 403 | Rol no admitido en esa plataforma/front |
| `Authentication.UserAlreadyExists` | 409 | Email ya registrado (también vía RegisterOwner) |
| `Authentication.IdentificationNumberAlreadyExists` | 409 | Cédula ya registrada (también vía RegisterOwner) |
| `Authentication.InvalidRegistrationData` | 400 | Datos de registro inválidos |

---

## ContactVerification.* — Gmail / proof (Telegram)

**Para quién:** bot Telegram (verificación de correo). **Qué no es:** OTP de cita (`AppointmentAction.*`).  
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
| `ContactVerification.PurposeInvalid` | 400 | Purpose inválido |
| `ContactVerification.EmailInvalid` | 400 | Formato de correo inválido |
| `ContactVerification.DeliveryFailed` | 409 | Fallo al enviar (SMTP); sin detalle de proveedor |
| `ContactVerification.NotImplemented` | 501 | Capacidad aún no implementada |

---

## Alta dueño — OwnerRegistration.* / Clients.*

**Para quién:** staff (`register-owner`) y bot (`/api/owners/bot`). **Qué no es:** pedir password al dueño; no es OTP de cita.  
**Fuentes:** [`OwnerRegistrationErrors.cs`](../../src/Application/Owners/Errors/OwnerRegistrationErrors.cs), [`ClientErrorCodes.cs`](../../src/Application/Clients/Errors/ClientErrorCodes.cs)

| code | HTTP | Cuándo |
|------|------|--------|
| `OwnerRegistration.ProofRequired` | 400 | Falta proof de ContactVerification |
| `OwnerRegistration.ProofEmailMismatch` | 400 | Proof no corresponde al email |
| `OwnerRegistration.ProofPurposeInvalid` | 400 | Purpose del proof incorrecto |
| `OwnerRegistration.ClientRoleMissing` | 409 | Rol Cliente no configurado en seed |
| `Clients.PhoneRequired` | 400 | Teléfono obligatorio (validación) |
| `Clients.PhoneInvalidFormat` | 400 | Formato de teléfono inválido |
| `Clients.PhoneAlreadyInUse` | 409 | Teléfono ya usado (`OwnerRegistration.PhoneAlreadyInUse` alias) |
| `Authentication.UserAlreadyExists` | 409 | Alias `OwnerRegistration.EmailAlreadyInUse` |
| `Authentication.IdentificationNumberAlreadyExists` | 409 | Alias `OwnerRegistration.IdentificationAlreadyInUse` |

---

## Telegram.Registration.* — enlace de alta

**Para quién:** bot Telegram. **Qué no es:** Gmail OTP ni OTP de cita.  
**Fuente:** [`TelegramRegistrationErrors.cs`](../../src/Application/Telegram/Registration/TelegramRegistrationErrors.cs)

| code | HTTP | Cuándo |
|------|------|--------|
| `Telegram.Registration.InvalidOrExpired` | 400 | Enlace inválido, usado o vencido |
| `Telegram.Registration.IdentityConflict` | 409 | Chat ya vinculado a otra identidad |

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
5. No añadir WhatsApp a este catálogo.
