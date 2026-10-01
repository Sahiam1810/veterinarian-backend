# Política de PII en Logs (Etapa 6.2)

## Objetivo
En lookup, Gmail/ContactVerification, RegisterOwner, Telegram y OTP de cita, los logs **no** escriben teléfono, cédula, código OTP, proof ni texto del correo. Se loguea el **resultado** (éxito/fallo + `code` + ids), no el secreto.

## PII prohibido
- Teléfono (`PhoneNumber`, requester, destino SMS)
- Email / cuerpo o asunto de correo
- OTP / código en claro
- Cédula / número de identificación
- Proof / completion token / link code

## Permitido
- Ids: `ClientId`, `UserId`, `AppointmentId`, `SessionId`
- `ErrorCode` / resultado de negocio
- Canal genérico (`Email`, `Sms`, `Telegram`) y `Purpose` / `Action` / `Mode` (Phone|Identification **sin** el valor)

## EF Core
`EnableSensitiveDataLogging` **no** se habilita (ni Production ni Development). Comentario de guardia en `Infrastructure/DependencyInjection.cs`.

## Inventario verificado
| Flujo | Traza segura |
|---|---|
| Lookup | `ClientId` + `Mode` |
| Gmail OTP | `SessionId` + `Purpose` + `Channel` |
| RegisterOwner | `UserId` + `ClientId` + `Channel` |
| Telegram | `ErrorCode` + `Attempt` / `ExceptionType` |
| OTP cita | `AppointmentId` + `SessionId` + `Action` (+ `ErrorCode` si falla envío) |

## Tests
`tests/Application.Tests/Security/PiiInLogsTests.cs` — logger fake (teléfono/correo/OTP no aparecen en mensajes) + escaneo de plantillas + guardia EF.

## Referencias
- ADR 6: `docs/adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md` §2
- ADR identidad OTP: `docs/adr/2026-09-04-client-identity-and-otp-boundaries.md`
- CONTEXT §0 / Etapa 6.2

## Fecha
2026-09-07
