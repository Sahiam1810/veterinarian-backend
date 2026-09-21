# Contrato de API v2 — separación de clientes y usuarios

Estado: **vigente para el refactor de clientes y usuarios** (backend, frontend y chatbot).
Alcance: solo lo que cambia por separar **clientes** (dueños de mascota) de **usuarios** (personal).

## 0. Cómo usar este documento

Este documento es la **fuente de verdad** de la reestructuración. Si el código actual lo contradice, gana este documento. Está escrito para personas y para asistentes de IA que trabajen una tarea del refactor.

Reglas para cualquier tarea de este refactor:

1. Trabaja **solo** lo que dice tu tarea. No adelantes ni corrijas cosas de otras tareas.
2. **No generes migraciones de EF** (`dotnet ef migrations add`) ni ejecutes `dotnet ef database update` ni SQL contra la base. La migración la genera una sola persona al cerrar el frente.
3. No cambies nombres de columnas o tablas por tu cuenta; los renombres son otro frente.
4. No agregues compatibilidad hacia atrás (alias, campos obsoletos): la base se recrea y los tres repos se despliegan juntos.
5. Los tests de tu parte deben quedar en verde antes de avisar que tu tarea está lista.

## 1. Contexto

Hoy un **cliente** son dos filas atadas: una en `USERS` (rol "Cliente", sin contraseña, con el nombre y el correo) y una en `CLIENTS` (cédula, teléfono, dirección, `USER_ID`).

Después del refactor:

- `CLIENTS` guarda **todos** los datos del cliente y **no tiene ninguna relación con `USERS`**.
- `USERS` (y sus cuentas y credenciales) quedan **solo para el personal**.
- El rol "Cliente" deja de existir en la base.
- Un cliente nunca inicia sesión; solo lo identifica el bot de Telegram.

## 2. Convenciones

- JSON en `camelCase`. Ids `guid` como texto. Fechas ISO-8601 en UTC.
- Errores de negocio: `application/problem+json` con un `code` estable. El frontend y el chatbot mapean por `code`, no por el mensaje.
- Las políticas de permisos de los endpoints del personal **no cambian**.

## 3. Recurso Cliente

| Campo | Tipo | Regla |
|---|---|---|
| `id` | guid | |
| `fullName` | string(150) | obligatorio |
| `email` | string(150) | obligatorio, **único**, **editable**; se guarda recortado y en minúsculas |
| `identificationNumber` | string(20) | obligatorio, único |
| `phoneNumber` | string(20) | **obligatorio**, único; se normaliza a solo dígitos (7 a 20) |
| `address` | string(150) | opcional |
| `isActive` | bool | |
| `createdAt`, `updatedAt` | fecha UTC | |

Desaparecen: `userId` y `registrationDate` (era redundante con `createdAt`).

## 4. Endpoints para el personal

| Endpoint | Body o respuesta |
|---|---|
| `GET /api/clients` | lista de Cliente |
| `GET /api/clients/{id}` | Cliente |
| `POST /api/clients` **(cambia)** | body `{ fullName, email, identificationNumber, phoneNumber, address? }` → **201** Cliente. Ya no recibe `userId` |
| `PUT /api/clients/{id}` **(cambia)** | body `{ fullName, email, identificationNumber, phoneNumber, address?, isActive }` → **204** |
| `DELETE /api/clients/{id}` | **204** (igual que hoy) |
| `GET /api/clients/lookup?identification=&phone=` (parámetros sin cambio) | Cliente completo (un solo objeto; 404 si no hay coincidencia) |

**Se eliminan:**

- `POST /api/clients/register-owner` (queda idéntico a `POST /api/clients`).
- `PUT /api/clients/{id}/owner-profile` (nombre y correo pasan a `PUT /api/clients/{id}`).
- `GET /api/clients/me` (hoy ya responde 410; portal retirado).

## 5. Búsquedas del bot (anónimas, con límite de peticiones; las rutas no cambian)

- `GET /api/clients/by-identification/{identificationNumber}` → **200** `{ id, identificationNumber, createdAt }` **(cambia)**. Sin `userId` ni `registrationDate`. También 404 y 429.
- `GET /api/clients/by-phone/{phone}` → misma respuesta.

## 6. Registro por el bot: `POST /api/owners/bot` (anónimo, con límite de peticiones)

- **Body** (igual que hoy): `{ fullName, email, identificationNumber, phoneNumber, address? }`. `phoneNumber` es obligatorio.
- **Respuesta 201 `{ clientId }`** **(cambia)**: sin `userId`.
- El endpoint inserta **solo en `CLIENTS`**. No crea usuario, cuenta ni credenciales.
- **Errores 409 (cambian de código, porque ya no son de autenticación):**
  - `Authentication.IdentificationNumberAlreadyExists` → `Clients.IdentificationAlreadyInUse`
  - `Authentication.UserAlreadyExists` → `Clients.EmailAlreadyInUse`
  - `Clients.PhoneAlreadyInUse` (se mantiene)
- **Errores 400:** `Clients.PhoneRequired` y `Clients.PhoneInvalidFormat` (se mantienen). También 429.

Los códigos de personal (`Authentication.*`) no cambian para el alta de usuarios del personal.

## 7. Vínculo con Telegram: `POST /api/integrations/telegram/bot-link`

- Sigue exigiendo el token de invitado con la claim `telegram_user_id`.
- **Body `{ clientId }`** **(cambia)** (antes `personId`). **Respuesta 200** `{ linkId }`. Errores 401, 403 y 409 como hoy.
- **Ya no se crea ninguna "cuenta fantasma"** en `USER_ACCOUNTS`.

## 8. Token delegado del bot para un cliente

`person_id` **se mantiene** en este frente porque lo exigen el chatbot (`jwt.py`, `bind_message_identity`) y los tokens del personal (notificaciones en tiempo real, `NotificationsController`, `AgentMessagesController`). Se elimina después, junto con la fusión de `USER_ACCOUNTS` y `USER_CREDENTIALS` en `USERS`, para tocar el chatbot una sola vez.

| Claim | Valor para el cliente |
|---|---|
| `sub` | **id del cliente** (hoy es el id de la cuenta) |
| `person_id` | **id del cliente** (mismo valor que `sub`) |
| `role` | `"Cliente"`, constante en el código |
| `role_id` | GUID fijo constante (el rol deja de existir en la base) |
| `preferred_username` | correo del cliente |
| `email` | correo del cliente |
| `iss`, `aud`, `jti`, `iat`, `nbf`, `exp` y el tipo de token delegado | igual que hoy |

- El token de **invitado** no cambia.
- El `userId` que el backend envía en cada mensaje al chatbot (`POST /api/v1/messages`) pasa a ser el **id del cliente**.
- Las rutas del bot (`/api/bot/appointments`, `/api/bot/pets`, etc.) **no cambian de ruta ni de respuesta**; solo cambia quién es la identidad.

## 9. Otros

- **Notificaciones:** la API para el personal no cambia. Los avisos de cliente son internos y no se listan por API.
- **`ChatConversationClientInfo`** (`clientId`, nombre, teléfono) mantiene su forma.
- **Chatbot, código sin uso:** `request_reschedule_code` y `confirm_reschedule_code` (`appointments.py`) solo los llaman los tests; se eliminan en la limpieza.
- **Participantes del chat (`/api/chat/participants`) (cambia):** `chatUserProfileId` y `aiModelId` desaparecen; se agrega `clientId`. Un participante tiene **exactamente una** identidad: `clientId` o `agentHumanId`.
  - Crear: `{ chatConversationId, participantTypeId, clientId?, agentHumanId? }`.
  - Respuesta: `{ id, chatConversationId, participantTypeId, clientId?, agentHumanId?, createdAt, updatedAt }`.
  - Cambiar identidad: `{ clientId?, agentHumanId? }`.
- **Perfiles de chat:** los endpoints `/api/chat/user-profiles` **se eliminan** (la tabla `CHAT_USER_PROFILES` desaparece).
- **Usuarios (`POST /api/users`) (cambia):** el rol "Cliente" ya no existe y ya no se aceptan `clientIdentificationNumber`, `clientPhoneNumber` ni `clientAddress`. La contraseña es siempre obligatoria.
- **Catálogo de códigos de error:** al aplicar los códigos nuevos, actualizar `docs/contracts/api-error-codes-catalog.md`.

## 9.1 Reclamación por OTP (invitado que escribe desde otro Telegram)

Aplica cuando alguien que **ya es cliente** escribe desde un Telegram que no está vinculado. Un cliente **nuevo** se registra y se vincula **sin OTP**.

**Regla de seguridad:** el código se envía **siempre al correo registrado del cliente**, resuelto por el servidor. El bot nunca elige el correo destino.

- `POST /api/contact-verification/email/request-claim-by-identification` (anónimo, con límite de peticiones). Body `{ identificationNumber }`. **202** `{ sessionId, expiresAt, channel, maskedEmail }`. Sin nombre ni ids del cliente. 404 si la cédula no existe.
- `POST /api/contact-verification/email/request` **ya no admite** el propósito `Claim` ni `subjectUserId`: solo `Register`. `Claim` responde 400 `ContactVerification.PurposeInvalid`.
- `POST /api/contact-verification/email/confirm` sin cambios: `{ sessionId, code }` → `{ sessionId, proof }` (un solo uso).
- **Vincular con prueba:** `POST /api/integrations/telegram/bot-link/claim`. Exige el token de invitado con `telegram_user_id`. Body `{ sessionId, proof }`. **200** `{ linkId, fullName }`. Consume el proof (propósito `Claim`); el cliente vinculado es el de la sesión. Errores: 400 (proof inválido), 401, 403, 409 (Telegram ya vinculado a otro cliente; proof ya usado o vencido).
- **`bot-link` con `{ clientId }`** queda **solo para el registro recién hecho**: el cliente debe haberse creado hace menos de `Telegram:RegistrationLinkWindowMinutes` (10 por defecto) y no tener un vínculo activo. Si no, 403 `Telegram.ClientLinkRequiresProof`.
- `POST /api/bot/pets/query-by-claim-proof` sin cambios (consulta temporal sin vincular).

## 10. Decisiones tomadas

1. `person_id` se mantiene hasta la fusión de usuarios.
2. `register-owner` y `owner-profile` se consolidan en `POST` y `PUT /api/clients`.
3. Los códigos de conflicto de cliente pasan a `Clients.*`.
4. `PUT /api/clients/{id}` incluye `isActive` como campo obligatorio.
5. El correo del cliente es obligatorio y editable; el teléfono es obligatorio.
