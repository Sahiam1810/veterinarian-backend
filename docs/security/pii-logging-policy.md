# Política de PII en Logs

## Objetivo
Proteger la información personal identificable (PII) de los usuarios en los logs del sistema para evitar exposición de datos sensibles.

## PII Prohibido en Logs
En los flujos de Lookup, Gmail, RegisterOwner, Telegram y OTP de cita, los logs NO deben escribir:

- **Teléfono**: `{PhoneNumber}`, `{phone}`, `{RequesterPhoneNumber}`
- **Email**: `{Email}`, `{email}`, `{EmailAddress}`
- **OTP/Código**: `{Otp}`, `{otp}`, `{OTP}`, `{Code}`, `{code}`
- **Cédula/Identificación**: `{IdentificationNumber}`, `{Cedula}`, `{IdNumber}`
- **Proof**: `{ContactProof}`, `{proof}`, `{VerificationProof}`

## Datos Permitidos en Logs
Los siguientes datos SÍ pueden aparecer en logs para mantener trazabilidad:

- **IDs**: `{SessionId}`, `{AppointmentId}`, `{UserId}`, `{ClientId}`
- **Códigos de error**: `{ErrorCode}`, `{ErrorType}`
- **Metadatos de flujo**: `{Purpose}`, `{Channel}`, `{Action}`
- **Contadores**: `{Count}`, `{Attempt}`
- **Técnicos**: `{ExceptionType}`, `{Status}`

## Flujos Verificados

### ✅ Lookup (GetClientLookupQueryHandler)
- **Estado**: Sin logs actuales
- **PII**: Ninguno

### ✅ Gmail (ContactEmailVerificationRequestHandler)
- **Logs existentes**: 
  - `"Fallo al enviar OTP de contacto. Purpose={Purpose}"`
  - `"OTP de contacto solicitado. SessionId={SessionId} Purpose={Purpose} Channel={Channel}"`
- **PII**: Ninguno ✅

### ✅ RegisterOwner (RegisterOwnerCommandHandler)
- **Estado**: Sin logs actuales
- **PII**: Ninguno

### ✅ Telegram (ProcessTelegramUpdate, TelegramUpdateWorker)
- **Logs existentes**:
  - `"Telegram update worker cycle failed with type {ExceptionType}"`
  - `"Telegram update processing failed with code {ErrorCode} on attempt {Attempt}."`
- **PII**: Ninguno ✅

### ✅ OTP de Cita (RequestAppointmentActionCodeCommand, ConfirmAppointmentActionCodeCommand)
- **Estado**: Sin logs actuales
- **PII**: Ninguno

## Test de Seguridad
Existe un test preventivo en `tests/Application.Tests/Security/PiiInLogsTests.cs` que documenta el estado actual de la política y puede extenderse para verificar automáticamente que no se agreguen logs con PII en el futuro.

## Referencias
- ADR: 2026-09-04-client-identity-and-otp-boundaries.md
- ADR: 2026-09-07-contact-verification-email-foundations.md
- ADR: 2026-09-07-register-owner-foundations.md

## Fecha
2026-09-08
