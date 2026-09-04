# Smoke — puerta de salida Etapa 2 (teléfono como llave)

## Objetivo

Marcar la etapa **Teléfono = llave operativa** como cerrada: create normaliza phone, rechazo de duplicados, lookup anónimo by-phone y lookup staff enriquecido.

Fuera de alcance: OTP de contacto, RegisterOwner, login Cliente.

## Checklist

| ID | Caso | Contrato | OK |
|----|------|----------|----|
| A | Create con phone normalizado | `CreateClientCommandHandlerTests.Handle_persists_the_client_with_a_normalized_phone_number` | [ ] |
| B | Phone duplicado → 409 + `Clients.PhoneAlreadyInUse` | `Handle_throws_conflict_when_phone_is_already_in_use` | [ ] |
| C | `GET /api/clients/by-phone/{phone}` anónimo 200/404/429 | `GetClientByPhone*` / `ClientPhoneLookup*` | [ ] |
| D | `GET /api/clients/lookup` staff 401/403/200 | `GetClientLookup*` / `ClientStaffLookup*` | [ ] |

## Comando (suite de aceptación)

```bash
dotnet test --filter "FullyQualifiedName~ClientsStage2AcceptanceTests|FullyQualifiedName~CreateClientCommandHandlerTests|FullyQualifiedName~GetClientByPhone|FullyQualifiedName~ClientPhoneLookup|FullyQualifiedName~GetClientLookup|FullyQualifiedName~ClientStaffLookup"
```

## Criterio de puerta

- Comando anterior en **verde** (0 fallos).
- Sin OTP ni RegisterOwner en esta suite.
- Matriz A–D marcada en `ClientsStage2AcceptanceTests` sin `[Fact(Skip)]`.

## Nota Oracle

La unicidad de teléfono se valida en Application (`ExistsByPhoneAsync`). Un índice único en BD puede añadirse en un follow-up tras limpiar duplicados legacy.
