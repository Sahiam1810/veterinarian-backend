# Smoke — Tarea 3.3 API HTTP anónima + rate limiting (Contact Email)

## Objetivo

Exponer **request** y **confirm** de contacto por HTTP **sin JWT**, con rate limit, OpenAPI y problem+json + `code`.  
Sin lógica de hash/envío en Api (controllers delgados → puertos Application 3.1/3.2).

## Endpoints

| Método | Ruta | Auth | Rate limit policy |
|--------|------|------|-------------------|
| POST | `/api/contact-verification/email/request` | `AllowAnonymous` | `ContactEmailRequest` |
| POST | `/api/contact-verification/email/confirm` | `AllowAnonymous` | `ContactEmailConfirm` |

- Request body: email + purpose (`Register`\|`Claim`). **No** WhatsApp/SMS.
- Responses: metadatos/`proof` **sin OTP** en claro.
- Errores tipados → problem+json (`ContactVerification.*` / `RateLimit.Exceeded`).

## Options

```bash
RateLimiting__ContactEmailRequestPermitLimit=5
RateLimiting__ContactEmailRequestWindowSeconds=60
RateLimiting__ContactEmailConfirmPermitLimit=10
RateLimiting__ContactEmailConfirmWindowSeconds=60
```

También en `src/Api/appsettings.json` y `.env.example`.

## Matriz HTTP (3.3)

| ID | Caso | Esperado | Test |
|----|------|----------|------|
| H1 | Request anónimo válido | 202 + sessionId/expiresAt/channel | `RequestEmail_ValidRegister_Returns202_WithSafeMetadata_WithoutOtp` |
| H2 | Email inválido | 400 + `ContactVerification.EmailInvalid` | `RequestEmail_InvalidEmail_Returns400_WithEmailInvalidCode` |
| H3 | Confirm sesión inexistente | 404 + `SessionNotFound` | `ConfirmEmail_UnknownSession_Returns404_WithSessionNotFoundCode` |
| H4 | Rate limit request | 429 + `RateLimit.Exceeded` | `RequestEmail_ExceedsRateLimit_Returns429_WithProblemJson` |
| H5 | Rate limit confirm | 429 + `RateLimit.Exceeded` | `ConfirmEmail_ExceedsRateLimit_Returns429_WithProblemJson` |

OpenAPI: atributos `EndpointSummary` / `EndpointDescription` / `ProducesResponseType` en `ContactVerificationController`.

## Comando

```bash
dotnet test --filter "FullyQualifiedName~ContactVerificationHttpTests"
```

## Criterio de hecho

- Sin JWT en request/confirm.
- 429 demostrable con problem+json.
- Controllers sin hash/SMTP.
- Sin endpoints WhatsApp/SMS de contacto.
