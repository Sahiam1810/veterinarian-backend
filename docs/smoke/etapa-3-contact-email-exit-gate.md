# Smoke — puerta de salida Etapa 3 (contacto Email / Gmail)

## Objetivo

Homogeneizar options de contacto y dejar red de regresión **Email → confirm → proof → resend/429** con fakes (sin Gmail real ni RegisterOwner).

Canal v1: **solo Email**. Fuera de alcance: WhatsApp/SMS/Twilio de contacto, RegisterOwner, Etapa 4 Telegram.

## Config (`ContactVerification`)

Distinta de `AppointmentVerification` y `Telegram`. Bind en `appsettings` + `.env.example`:

| Clave | Rol |
|-------|-----|
| `ContactVerification__OtpTtlMinutes` | TTL del OTP |
| `ContactVerification__OtpMaximumAttempts` | Tope de intentos → `Blocked` |
| `ContactVerification__OtpResendSeconds` | Ventana de resend → `ResendTooSoon` |
| `ContactVerification__ProofTtlMinutes` | TTL del proof single-use |
| `ContactVerification__OtpPepperBase64` | Pepper propio; vacío reutiliza Appointment/Telegram |

Rate limit HTTP (429): `RateLimiting__ContactEmailRequestPermitLimit` / `ContactEmailConfirmPermitLimit`.

## Matriz de aceptación (solo Email)

| ID | Caso | Code / HTTP | Contrato |
|----|------|-------------|----------|
| A | Request fake → Confirm → Proof → Consume | OK | `ContactEmailVerificationAcceptanceTests.Acceptance_Email_Confirm_Proof_Consume_Success` |
| B | OTP inválido / bloqueo | `ContactVerification.InvalidCode` / `Blocked` | `Acceptance_InvalidOtp_Then_Blocked_UsesCatalogCodes` |
| C | Resend prematuro | `ContactVerification.ResendTooSoon` | `Acceptance_ResendTooSoon_ReturnsCatalogCode` |
| D | OTP expirado | `ContactVerification.Expired` | `Acceptance_ExpiredOtp_ReturnsExpiredCode` |
| E | Proof 2.º consume | `ContactVerification.ProofAlreadyConsumed` | `Acceptance_SecondProofConsume_ReturnsAlreadyConsumed` |
| F | Rate limit request | HTTP 429 | `ContactVerificationHttpTests.RequestEmail_ExceedsRateLimit_Returns429` |
| G | Options positivas / inválidas | bind validator | `ContactVerificationOptionsValidatorTests` |

## Comando

```bash
dotnet test --filter "FullyQualifiedName~ContactEmailVerificationAcceptanceTests|FullyQualifiedName~ContactVerificationHttpTests|FullyQualifiedName~ContactVerificationOptionsValidatorTests|FullyQualifiedName~ConfirmAndConsumeContactEmailVerificationTests"
```

## Criterio de puerta

- Comando anterior en **verde** (0 fallos).
- Sin SMTP/Gmail real: fake mailbox + stub/fake request en tests.
- Sin RegisterOwner ni canal WhatsApp/SMS de contacto.
