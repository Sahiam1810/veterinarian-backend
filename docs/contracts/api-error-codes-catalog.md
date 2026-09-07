# Catálogo de `code` — API problem+json

**Propósito:** el front **staff** traduce UX solo por `code` (no por `title` / `message` / `Description`).  
**OTP:** no hay OTP en la web staff. OTP Gmail y OTP de cita son flujos anónimos/chatbot.

**Doc de etapa:** [`docs/adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md`](../adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md).

## Dónde vive la fuente de verdad

| Área | Archivo en código |
|------|-------------------|
| Auth / plataforma | `src/Application/Security/Errors/AuthenticationErrors.cs` |
| Portal Cliente retirado | `src/Application/Security/Errors/ClientPortalErrors.cs` |
| Contact verification (Gmail) | `src/Application/ContactVerification/Errors/ContactVerificationErrors.cs` |
| RegisterOwner | `src/Application/Owners/Errors/OwnerRegistrationErrors.cs` |
| Telegram registration | `src/Application/Telegram/Registration/TelegramRegistrationErrors.cs` |

Nuevo `code` → agregar constante/`Error` en el `*Errors.cs` correspondiente **y** una fila aquí (tarea de codes, no el kickoff).

## Forma de respuesta (cuando el pipeline emite problem+json)

```json
{
  "type": "https://httpstatuses.com/401",
  "title": "Unauthorized",
  "status": 401,
  "code": "Authentication.InvalidCredentials"
}
```

Algunos endpoints legacy aún usan `ApiErrorResponse` sin `code`. No inventar `code` ad hoc en el controller: migrar vía excepción tipada + handler.

## Codes conocidos (inventario kickoff)

### Authentication (`AuthenticationErrors.cs`)

| code | HTTP típico |
|------|-------------|
| `Authentication.InvalidCredentials` | 401 |
| `Authentication.InvalidRefreshToken` | 401 |
| `Authentication.Unauthorized` | 401 |
| `Authentication.Forbidden` | 403 |
| `Authentication.PlatformAccessDenied` | 403 |
| `Authentication.UserAlreadyExists` | 409 |
| `Authentication.IdentificationNumberAlreadyExists` | 409 |
| `Authentication.InvalidRegistrationData` | 400 |

### Client portal (`ClientPortalErrors.cs`)

| code | HTTP típico |
|------|-------------|
| `ClientPortal.Gone` | 410 |

### Contact verification / Owner / Telegram

Ver constantes en los `*Errors.cs` listados arriba. Al alinear 6.3, copiar cada `code` literal a esta tabla sin cambiar el string.

## Reglas

1. Staff UI: switch/i18n por `code` únicamente.
2. No loguear el `Description` si contiene datos de negocio sensibles; en logs usar `code` + ids.
3. Rate limit 429: preferir un `code` estable si el pipeline lo expone; no inventar uno distinto por ruta sin entrada en catálogo.
