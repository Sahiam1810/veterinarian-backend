# Smoke — puerta de salida Etapa 4 (RegisterOwner)

## Objetivo

Matriz del **núcleo** RegisterOwner con fakes (repos + ConsumeProof + adaptadores Application).
CI debe fallar si alguien vuelve a persistir password, `USER_ACCOUNTS` o `USER_CREDENTIALS` de Cliente.

No espera 4.1–4.4 en develop. La corrida conjunta HTTP staff + bot + Telegram es cierre de etapa del equipo, no condición de merge de 4.5.

Fuera de alcance: Gmail/SMTP, Twilio, WhatsApp, request OTP (Etapa 3), borrar Telegram.

## Flag (`RegisterOwner`)

| Clave | Rol |
|-------|-----|
| `RegisterOwner__RequireContactProofs` | Solo **staff**. Default `false` (ADR: escritorio). `true` exige proof Email. Bot siempre ConsumeProof. Telegram: equivalencia OTP de sesión (sin ContactVerification). |

## Matriz de aceptación

| ID | Caso | Esperado | Test |
|----|------|----------|------|
| A | Staff, flag false (ADR) | User Cliente, `PASSWORD_HASH` null, 0 accounts/creds, sin ConsumeProof | `Acceptance_Staff_WithoutProofFlag_CreatesClienteWithoutPasswordOrLogin` |
| B | Staff, flag true + proof Register | ConsumeProof 1 vez, hash null, 0 accounts | `Acceptance_Staff_WithProofFlag_ConsumesEmailProofAndDoesNotCreateLogin` |
| C | Bot + proof | ConsumeProof; canal Bot | `Acceptance_BotAdapter_AlwaysConsumesRegisterProof` |
| D | Telegram sin ContactProof (equivalencia sesión) | sin ConsumeProof; hash null; 0 accounts | `Acceptance_TelegramAdapter_UsesSessionEquivalence_WithoutContactProof` |
| E | Login del email registrado (0 accounts) | Sin JWT | `Acceptance_RegisteredEmail_LoginWithoutAccount_DoesNotIssueJwt` |
| F | Login Cliente si hubiera account+password | `Authentication.PlatformAccessDenied` | `Acceptance_ClienteWithLegacyLogin_IsPlatformAccessDenied` |
| G | Alta de account sobre el user registrado | `Authentication.PlatformAccessDenied` | `Acceptance_CreateUserAccount_ForRegisteredCliente_IsPlatformAccessDenied` |
| H | Email / cédula / teléfono duplicados | conflicto con codes de catálogo | `Acceptance_DuplicateEmail_IsConflict`, `...Identification...`, `...Phone...` |
| I | Proof Claim no registra | `OwnerRegistration.ProofPurposeInvalid` | `Acceptance_ClaimProof_DoesNotRegisterOwner` |
| J | Segundo ConsumeProof | no reutiliza (fake single-use) | `Acceptance_ConsumedProof_CannotRegisterAgain` |

## Comando

```bash
dotnet test --filter "FullyQualifiedName~RegisterOwnerStage4AcceptanceTests|FullyQualifiedName~RegisterOwnerLoginDeniedAcceptanceTests"
```

Matriz Application (sin JWT):

```bash
dotnet test --filter FullyQualifiedName~Owners.Acceptance.RegisterOwnerStage4AcceptanceTests
```

## Criterio de puerta

- Comandos anteriores en **verde**.
- Sin proveedores externos (no SMTP/Gmail/Twilio/WhatsApp).
- Sin llamadas HTTP de 4.1–4.3.
