# Refactor: separación de clientes y usuarios

Estado: **en curso** (backend, frontend y chatbot).
Documento hermano con el detalle de la API: [`docs/contracts/clientes-usuarios-api-v2.md`](contracts/clientes-usuarios-api-v2.md).

Este documento da el **contexto general** del refactor para las personas y los asistentes de IA que trabajan una tarea. Léelo junto con el contrato de API antes de empezar.

## 1. Qué estamos haciendo y por qué

Hoy la base de datos mezcla dos conceptos:

- **Usuarios** (personal de la clínica): inician sesión con correo y contraseña.
- **Clientes** (dueños de mascota): nunca inician sesión; solo los identifica el bot de Telegram.

Actualmente un cliente son **dos filas atadas**: una en `USERS` (rol "Cliente", sin contraseña, con nombre y correo) y una en `CLIENTS` (cédula, teléfono, dirección). Eso obliga a que el bot, Telegram, las notificaciones y el chat dependan de `USERS`.

El objetivo es **separarlos por completo**:

- `CLIENTS` guarda **todos** los datos del cliente y **no tiene ninguna relación con `USERS`**.
- `USERS` (y sus cuentas y credenciales) quedan **solo para el personal**.
- El rol "Cliente" deja de existir en la base.
- Aprovechamos para dejar la base limpia: de **58 a 33 tablas**, quitando lo que nunca se usa ni se muestra.

## 2. Modelo objetivo (resumen)

El DBML completo está en el **Anexo A** (se puede pegar en https://dbdiagram.io).

Estado del modelo: **boceto presentado para aprobación**. Si se ajusta, se actualiza el anexo.

| Grupo | Tablas |
|---|---|
| Usuarios y seguridad (personal) | `USERS`, `USER_TOKENS`, `ROLES`, `ROLE_PERMISSIONS`, `MODULES` |
| Clientes y mascotas | `CLIENTS`, `CLIENTS_PETS`, `PETS`, `SPECIES`, `RACES` |
| Personal clínico | `VETERINARIANS`, `SPECIALTIES`, `AVAILABILITIES`, `VETERINARIAN_ABSENCES` |
| Servicios y citas | `TYPE_SERVICES`, `SERVICES`, `STATUS_APPOINTMENTS`, `APPOINTMENTS`, `APPOINTMENT_STATUS_HISTORIES`, `NOTIFICATIONS` |
| Historia clínica | `DIAGNOSTICS`, `MEDICAL_RECORDS`, `VACCINATIONS` |
| Chat y escalamiento | `AGENT_HUMANS`, `CHAT_CONVERSATIONS`, `CHAT_PARTICIPANTS`, `CHAT_MESSAGES`, `CHAT_ESCALATIONS`, `SENDER_TYPES`, `ESCALATIONS_STATUSES` |
| Telegram | `TELEGRAM_USER_LINKS`, `TELEGRAM_INBOUND_UPDATES`, `CONTACT_VERIFICATION_SESSIONS` |

Cambios clave respecto a hoy:

- **`CLIENTS`** gana `FULL_NAME`, `EMAIL` (obligatorio, único, editable) e `IS_ACTIVE`; el teléfono es obligatorio; pierde `USER_ID` y `REGISTRATION_DATE`.
- **`USERS`** absorbe `USER_ACCOUNTS` y `USER_CREDENTIALS` (contraseña incluida). El login es solo por correo (sin `USERNAME`).
- **`TELEGRAM_USER_LINKS`, `CHAT_PARTICIPANTS` y `NOTIFICATIONS`** apuntan a `CLIENTS` (`CLIENT_ID`). Las notificaciones tienen `USER_ID` **o** `CLIENT_ID`, uno solo.
- **`CHAT_ESCALATIONS`** absorbe la resolución (`RESOLVED_AT`, `RESOLVED_BY`, `RESOLUTION_NOTE`); `TELEGRAM_USER_LINKS` absorbe el vínculo con la conversación.
- **Tablas que se eliminan:** `USER_ACCOUNTS`, `USER_CREDENTIALS`, `USER_PERMISSIONS`, `ACCOUNT_STATEMENTS`, `AI_MODELS`, `PROVIDER_MODELS_AI`, `AI_RUNS_STATUSES`, `CHAT_AI_RUNS`, `CHAT_AI_RUN_ERRORS`, `CHAT_AI_RUN_METRICS`, `CHAT_CONVERSATION_AI_SETTINGS`, `CHAT_ATTACHMENTS`, `CHAT_CONVERSATION_ASSIGNMENTS`, `CHAT_ESCALATION_ASSIGNMENTS`, `CHAT_ESCALATION_STATUS_HISTORY`, `CHAT_ESCALATION_RESOLUTION`, `CHAT_USER_PROFILES`, `MESSAGE_TYPES`, `CONVERSATIONS_STATUSES`, `PRIORITY`, `APPOINTMENT_ACTION_VERIFICATION_SESSIONS`, `TELEGRAM_CONVERSATION_LINKS`, `TELEGRAM_LINK_CODES`, `TELEGRAM_LINKING_SESSIONS`, `TELEGRAM_REGISTRATION_SESSIONS`.
- **Tabla que se conserva: `CONTACT_VERIFICATION_SESSIONS`.** Un borrador anterior de esta lista la eliminaba; **se decidió dejarla**. Guarda las sesiones del OTP por correo (código con hash, intentos, vencimiento y la prueba de un solo uso), y ese flujo sigue vigente: se usa **únicamente cuando alguien que ya es cliente escribe desde otra cuenta de Telegram** que no está vinculada, para demostrar que controla el correo registrado (contrato v2, sección 9.1). Un cliente **nuevo** se registra y se vincula sin OTP, y no pasa por esta tabla. Si se borrara, el bot no tendría cómo reconocer a un cliente que cambia de celular sin abrir el flujo a suplantaciones. **Ninguna tarea de limpieza debe borrarla, ni el módulo `ContactVerification` que la usa.**
  - Pendiente para el frente de renombres: su columna `SUBJECT_USER_ID` hoy guarda el **id del cliente** (no tiene FK). Debe pasar a `SUBJECT_CLIENT_ID`.
- **Atributos:** booleanos como `NUMBER(1)`, `UPDATE_AT` pasa a `UPDATED_AT`, tipos homogéneos y nombres de id en singular. Esto es de un frente posterior, **no** del frente actual.

## 3. Frentes

El trabajo se hace por frentes; **cada frente cierra con una migración** que genera una sola persona.

**Frente 1 (actual): separar clientes de usuarios.** Cierra con la migración `SeparateClientsFromUsers`.
Siguientes frentes: fusionar usuarios y quitar permisos por usuario; borrar las tablas sobrantes; renombres y booleanos; fusionar resolución y vínculo de conversación.

**Frente 2 (siguiente): fusionar usuarios y quitar permisos por usuario.** Cierra con la migración `MergeUsersAndAccounts`. Contrato: [`docs/contracts/fusion-usuarios-api-v3.md`](contracts/fusion-usuarios-api-v3.md). Se empieza cuando el Frente 1 está mergeado y migrado. `ACCOUNT_STATEMENTS` y los flujos viejos de Telegram por código y por registro web pasan del frente de "tablas sobrantes" a este, porque dependen de `USER_ACCOUNTS`.

| Tarea | Qué es | Depende de |
|---|---|---|
| **U1** | Eliminar permisos por usuario (`USER_PERMISSIONS`); permisos solo por rol | nada |
| **U2** | Eliminar el módulo `AccountStatements` | nada |
| **U3** | Eliminar Telegram por código y por registro web (`TELEGRAM_LINK_CODES`, `TELEGRAM_LINKING_SESSIONS`, `TELEGRAM_REGISTRATION_SESSIONS`) | nada |
| **U4** | `sub` del token = id del usuario; casos de uso del personal pasan de `UserAccountId` a `UserId` (las cuentas aún existen) | nada |
| **U5** | Fusión: `USERS` con contraseña, login por correo, refresh tokens por usuario, se borran cuentas y credenciales y sus endpoints | U2, U3 y U4 |
| **U6** | Eliminar la claim `person_id` | U4, U5 y **CU1 desplegado** |
| **U7** | Tests y documentación del frente | U6 |
| **E2** | Seed de desarrollo sin cuentas ni credenciales | U5 (se prueba con la migración aplicada) |
| **FU1 + FU2** | Frontend: alta de usuario en un solo paso, fuera cuentas, credenciales y permisos por usuario (misma persona: tocan el mismo hook) | contrato v3 |
| **FU3** | Frontend: `personId`, `accountId`, `userAccountId` y `userName` fuera de tipos, perfil y sesión | contrato v3 |
| **FU4** | Frontend: tests y `lint` | FU1, FU2 y FU3 |
| **CU1** | Chatbot: deja de exigir `person_id`; `userId` == `sub` | nada (**va primero**) |
| **CU2** | Chatbot: documentación y tests | CU1 |

### Tareas del Frente 1

**Backend**

| Tarea | Qué es |
|---|---|
| **T1** | Núcleo de Cliente: `ClientEntity`, configuración EF y repositorio (incluye lo que antes era T2). Primera y desbloquea a las demás |
| **T3-T4** | Un solo ticket. Casos de uso, DTO, mapeos y controladores de clientes sin `userId`; quitar `register-owner`, `owner-profile` y `me`. `RegisterOwner` crea solo el cliente y `POST /api/owners/bot` devuelve `{ clientId }`. **`ClientEntity.UserId` pasa a opcional** (lo borra la T11) |
| **T5-T6** | Un solo ticket. Telegram: `TelegramUserLink` apunta al cliente; `bot-link` recibe `clientId`; sin "cuenta fantasma". Identidad del bot desde `CLIENTS` (claims de la sección 8 del contrato) |
| T7 | Handlers "Mis…" resuelven al cliente por la claim del token |
| T8 | Notificaciones con `UserId` o `ClientId`; recordatorios de Telegram por `ClientId` |
| T9 | Chat: `ChatParticipant` con `ClientId` (sin `AiModelId`); quitar el módulo `ChatUserProfile` |
| T10 | Lado usuarios: `DeleteUser` y `CreateUser` sin lógica de clientes; eliminar el rol "Cliente" de seeds y código; sin bloqueo de login por rol Cliente. (La creación del cliente dentro de `CreateUser` ya la quita la T3-T4) |
| **T11** | **Última:** borrar `UserId`/`User` de `ClientEntity` y los métodos `...ByUserId` |
| **T12-A** | Documentación: `docs/API.md`, sección de dueños del `README`, `docs/contracts/api-error-codes-catalog.md` y la guía de Telegram. No se tocan los ADR. Solo toca `docs/` y `README.md`; puede empezar ya |
| **T12-B** | Tests: barrido final de la suite y pruebas de aceptación del frente. Va después de la T11 |
| **E1** | Seed de desarrollo: reescribir `insert_all_seeds.sql`, `limpieza_total.sql` y las reglas de `verify_seeds.sql` (personal + clientes de demo con el modelo nuevo). Solo SQL; se prueba con la migración ya aplicada |

**Frontend** (repo `veterinarian-fronted`; ramas `refactor/F<n>-...` desde su `develop`):

| Tarea | Qué es | Depende de (en el frontend) |
|---|---|---|
| **F1** | Servicio de clientes (`superAdminClientsService.ts`) y pantallas de SuperAdmin que lo usan: dueños, agenda y profesionales. Quita `userId` y `registrationDate` | nada |
| **F2** | Panel de Recepcionista: dueños (servicio, mapeo, hook, formulario) | **F1** (importa el servicio) |
| **F3** | Tipos sueltos: auxiliar, veterinario, participantes de chat; y códigos de error en `toSpanishAuthError.ts` | nada |
| **F4** | SuperAdmin › Usuarios: quitar el alta de "Cliente" desde el formulario de usuarios | nada (mergear antes que F1) |
| **F5** | Tests y `lint` del frontend | F1, F2, F3 y F4 |

Ninguna tarea del frontend necesita código del backend para escribirse (se guían por el contrato). Para **probar en vivo** necesitan, en `develop` del backend y con la migración aplicada: T3-T4 (F1, F2), T9 (F3, participantes) y T10 (F4).

**Chatbot** (repo `Huellitas_ChatBot`; ramas `refactor/C<n>-...` desde su `develop`). La C3 se eliminó: la validación del token no cambia en este frente (`person_id` sigue existiendo y `userId` == `person_id` == id del cliente).

| Tarea | Qué es | Depende de (en el chatbot) |
|---|---|---|
| **C1** | Puerto y adaptador de identidad de invitado: búsqueda, registro y `bot-link` con `clientId`; sus tests | nada |
| **C2** | Coordinador de identificación (`guest_identification.py`): códigos de conflicto nuevos y `client_id`; sus tests | **C1** (usa el puerto) |
| **C4** | Documentación (`README`, guía JWT, arquitectura), código muerto de códigos OTP de reprogramación y corrida final de la suite | nada (la corrida final, después de C1 y C2) |

Para probar en vivo necesitan el backend en `develop` con la migración: T3-T4 (`owners/bot` y búsqueda), T5-T6 (`bot-link` y token del cliente).

**Orden y dependencias del backend.** Frontend y chatbot arrancan desde el contrato sin esperar al backend.

| Tarea | Debe estar en `develop` antes de empezar | Conviene mergear después de |
|---|---|---|
| T1 | nada | nada |
| T3-T4 | T1 | nada |
| T5-T6 | T1 | nada |
| T7 | T1 | T3-T4 (comparten archivos de tests de clientes) |
| T8 | T1, **T5-T6** (usa `GetByClientIdAsync`) | T3-T4 (ambas tocan `GenerateUpcomingAppointmentReminders...`) |
| T9 | T1 | T5-T6 (comparten `PersistentConversationContextProvider` y `ChatConversationClientResolver`) |
| T10 | T1 | T3-T4 (`CreateUserCommandHandler`), T5-T6 (la identidad del cliente reutiliza `WebPlatformAccess.ClientRoleName`) y T9 (`DeleteUserCommandHandler`) |
| T11 | T1, T3-T4, T5-T6, T7, T8, T9 y T10 | (va al final, antes de la migración) |
| T12-A | nada (solo documentación; el contrato ya está fijado) | puede ir en paralelo a todo |
| T12-B | T11 | (va después de la T11) |
| E1 | T1 (para conocer las columnas de `CLIENTS`); se **prueba** con la migración aplicada | debe estar en `develop` **antes** de que se avise al equipo de hacer `database update` |

Orden de merge sugerido: T1; T3-T4 y T5-T6; T7; T8; T9; T10; T11; T12-B (la T12-A entra cuando esté lista).

## 4. Flujo de trabajo y ramas

No hay rama compartida: **cada persona crea su propia rama desde `develop`** y la responsable del esquema hace el merge a `develop`.

1. **Cada persona crea su rama desde `develop`** actualizada, con el nombre `refactor/T<número>-descripcion-corta`:
   ```powershell
   git checkout develop
   git pull
   git checkout -b refactor/T1-cliente-entidad-repositorio
   ```
2. Trabaja **solo** su tarea, deja los tests en verde y sube la rama (`git push -u origin refactor/T1-cliente-entidad-repositorio`).
3. **Avisa a la responsable del esquema:** "subí la tarea T1, está en la rama `refactor/T1-cliente-entidad-repositorio`".
4. La responsable **revisa la rama y hace el merge a `develop`**.
5. **Migraciones:** solo las genera la responsable del esquema, después del merge. Luego avisa al equipo: `git pull` de `develop` y `dotnet ef database update`. Si falla por datos existentes, se resetea la base local.
6. **Orden en el backend:** la T1 se revisa y se une a `develop` **primero**. Las demás tareas de backend (T3-T4 a T10) crean su rama desde `develop` **después** de que la T1 esté ahí, porque parten de la entidad ya cambiada. La T11 va al final. Frontend y chatbot pueden empezar desde `develop` desde el principio, porque dependen del contrato y no del código del backend.
7. Entre el merge de una tarea de backend con cambios de modelo y la migración, la API no corre contra una base sin migrar (los tests sí). La responsable avisa cuando `develop` está en esa ventana.

## 5. Reglas para cualquier tarea

- Haz **solo** lo que dice tu tarea; no adelantes ni corrijas cosas de otras.
- **No generes migraciones** (`dotnet ef migrations add`), no ejecutes `dotnet ef database update` ni SQL contra la base. Tu PR **no** debe incluir `src/Infrastructure/Migrations/`.
- No renombres columnas ni tablas por tu cuenta.
- No agregues compatibilidad hacia atrás (alias, campos obsoletos): la base se recrea y los tres repos se despliegan juntos.
- Si el código contradice el contrato de API, **gana el contrato**.
- Deja los tests de tu parte en verde antes de avisar que tu tarea está lista.

## 6. Glosario

| Término | Significado en este refactor |
|---|---|
| Usuario | Personal de la clínica (SuperAdmin, Administrador, Recepcionista, Veterinario, Auxiliar). Inicia sesión |
| Cliente | Dueño de mascota. Nunca inicia sesión; lo identifica el bot |
| `person_id` | Claim del JWT. Para el cliente vale igual que `sub` (id del cliente). Se elimina en el frente de fusión de usuarios |
| Cuenta fantasma | Fila de `USER_ACCOUNTS` sin contraseña que hoy se crea al vincular Telegram. Desaparece |
| Responsable del esquema | Quien revisa las ramas, las une a `develop` y genera las migraciones |

## 7. Fuera de alcance (backlog posterior)

- Módulo de vacunación con esquemas (hoy `VACCINATIONS` cuelga de la consulta médica).
- Mostrar **todos** los chats y las respuestas del bot en el panel del asesor (hoy solo los escalados, y las respuestas del bot no se guardan).
- Pantalla de ausencias de veterinarios (el backend está listo; falta el frontend).
- Mover al backend la creación de `AGENT_HUMANS` y de participantes, que hoy hace el frontend.
- Decidir qué hacer con `POST /api/agent/messages` (chat web autenticado): el frontend no lo usa y el modelo final (`CHAT_PARTICIPANTS` solo de cliente o agente humano) no soporta conversaciones iniciadas por personal. Propuesta: eliminarlo en la limpieza. Mientras tanto queda sin cambios y solo funciona para identidades de cliente.

## 8. Documentos relacionados

- [`docs/contracts/clientes-usuarios-api-v2.md`](contracts/clientes-usuarios-api-v2.md): contrato de API del frente actual.
- [`docs/contracts/api-error-codes-catalog.md`](contracts/api-error-codes-catalog.md): catálogo de códigos de error (se actualiza en la T12-A).
- [`docs/API.md`](API.md): catálogo de endpoints (se actualiza en la T12-A).

## Anexo A — Modelo objetivo (DBML)

Pégalo tal cual en https://dbdiagram.io.

```dbml
// Huellitas — BASE DE DATOS REESTRUCTURADA (propuesta para aprobacion)
// Pegar tal cual en https://dbdiagram.io  ·  33 tablas (antes 58)
// Cambios clave: USERS = solo personal (fusiona cuentas y credenciales) · CLIENTS independiente, sin login y sin FK a USERS

Table USERS {
  USER_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  EMAIL "varchar2(150)" [not null, unique, note: 'usuario inicia sesion con este correo']
  FULL_NAME "varchar2(150)" [not null]
  IS_ACTIVE "number(1)" [not null]
  PASSWORD_HASH "varchar2(255)" [not null]
  PASSWORD_CHANGED_AT "timestamp"
  PHOTO_URL "varchar2(500)"
  ROLE_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
}

Table USER_TOKENS {
  TOKEN_ID "varchar2(36)" [pk]
  USER_ID "varchar2(36)" [not null]
  CREATED_AT "timestamp" [not null]
  EXPIRES_AT "timestamp" [not null]
  SESSION_STARTED_AT "timestamp" [not null]
  TOKEN_TYPE "varchar2(20)" [not null]
  TOKEN_VALUE "varchar2(500)" [not null]
  UPDATED_AT "timestamp"
}

Table ROLES {
  ROLE_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  DESCRIPTION "clob"
  NAME "varchar2(50)" [not null]
}

Table ROLE_PERMISSIONS {
  ROLE_PERMISSION_ID "varchar2(36)" [pk]
  CAN_CREATE "number(1)" [not null]
  CAN_DELETE "number(1)" [not null]
  CAN_EDIT "number(1)" [not null]
  CAN_VIEW "number(1)" [not null]
  CREATED_AT "timestamp" [not null]
  MODULE_ID "varchar2(36)" [not null]
  ROLE_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
}

Table MODULES {
  MODULE_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  DESCRIPTION "clob"
  NAME "varchar2(50)" [not null]
  UPDATED_AT "timestamp"
}

Table CLIENTS {
  CLIENT_ID "varchar2(36)" [pk]
  FULL_NAME "varchar2(150)" [not null]
  EMAIL "varchar2(150)" [not null, unique]
  ADDRESS "varchar2(150)"
  CREATED_AT "timestamp" [not null]
  IDENTIFICATION_NUMBER "varchar2(20)" [not null, unique]
  PHONE_NUMBER "varchar2(20)" [not null, unique]
  UPDATED_AT "timestamp"
  IS_ACTIVE "number(1)" [not null]
}

Table CLIENTS_PETS {
  CLIENT_PET_ID "varchar2(36)" [pk]
  CLIENT_ID "varchar2(36)" [not null]
  CREATED_AT "timestamp" [not null]
  IS_PRIMARY_OWNER "number(1)" [not null]
  PET_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
}

Table PETS {
  PET_ID "varchar2(36)" [pk]
  AGE "number(10)" [not null]
  CREATED_AT "timestamp" [not null]
  GENDER "varchar2(1)" [not null]
  NAME "varchar2(50)" [not null]
  OBSERVATIONS "varchar2(500)"
  PHOTO_URL "varchar2(500)"
  RACE_ID "varchar2(36)" [not null]
  SPECIES_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
  WEIGHT "number(6,3)" [not null]
}

Table SPECIES {
  SPECIES_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  NAME "varchar2(20)" [not null]
  UPDATED_AT "timestamp"
}

Table RACES {
  RACE_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  NAME "varchar2(20)" [not null]
  SPECIES_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
}

Table VETERINARIANS {
  VETERINARIAN_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  LICENSE_NUMBER "varchar2(20)" [not null]
  SPECIALTY_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
  USER_ID "varchar2(36)" [not null]
}

Table SPECIALTIES {
  SPECIALTY_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  DESCRIPTION "varchar2(120)"
  NAME "varchar2(120)" [not null]
  UPDATED_AT "timestamp"
}

Table AVAILABILITIES {
  AVAILABILITY_ID "varchar2(36)" [pk]
  CONSULTING_ROOM "varchar2(50)"
  CREATED_AT "timestamp" [not null]
  DAY_OF_WEEK "number" [not null]
  END_TIME "varchar2(30)" [not null]
  IS_ACTIVE "number(1)" [not null]
  MAX_CONCURRENT_APPOINTMENTS "number(10)" [not null]
  SHIFT_NAME "varchar2(30)"
  SLOT_DURATION_MINUTES "number(10)" [not null]
  START_TIME "varchar2(30)" [not null]
  UPDATED_AT "timestamp"
  VETERINARIAN_ID "varchar2(36)" [not null]
}

Table VETERINARIAN_ABSENCES {
  ABSENCE_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  END_AT "timestamp" [not null]
  IS_FULL_DAY "number(1)" [not null]
  REASON "varchar2(200)"
  START_AT "timestamp" [not null]
  UPDATED_AT "timestamp"
  VETERINARIAN_ID "varchar2(36)" [not null]
}

Table TYPE_SERVICES {
  TYPE_SERVICE_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  DESCRIPTION "varchar2(200)"
  NAME "varchar2(50)" [not null]
  UPDATED_AT "timestamp"
}

Table SERVICES {
  SERVICE_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  DURATION_MINUTES "number" [not null]
  IS_ACTIVE "number(1)" [not null]
  NAME "varchar2(50)" [not null]
  PRICE "number" [not null]
  TYPE_SERVICE_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
}

Table STATUS_APPOINTMENTS {
  STATUS_APPOINTMENT_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  DESCRIPTION "varchar2(200)"
  NAME "varchar2(50)" [not null]
  UPDATED_AT "timestamp"
}

Table APPOINTMENTS {
  APPOINTMENT_ID "varchar2(36)" [pk]
  AVAILABILITY_ID "varchar2(36)" [not null]
  BOOKING_REQUEST_KEY_HASH "varchar2(64)"
  CLIENT_PET_ID "varchar2(36)" [not null]
  CONSULTING_ROOM "varchar2(50)"
  CREATED_AT "timestamp" [not null]
  NOTES "varchar2(500)"
  REQUESTER_PHONE_NUMBER "varchar2(20)"
  SCHEDULED_END "timestamp" [not null]
  SCHEDULED_START "timestamp" [not null]
  SERVICE_ID "varchar2(36)" [not null]
  STATUS_APPOINTMENT_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
  VETERINARIAN_ID "varchar2(36)" [not null]
}

Table APPOINTMENT_STATUS_HISTORIES {
  APPOINTMENT_STATUS_HISTORY_ID "varchar2(36)" [pk]
  APPOINTMENT_ID "varchar2(36)" [not null]
  CLIENT_PET_ID "varchar2(36)" [not null]
  COMMENT "varchar2(100)"
  CREATED_AT "timestamp" [not null]
  STATUS_APPOINTMENT_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
}

Table NOTIFICATIONS {
  NOTIFICATION_ID "varchar2(36)" [pk]
  APPOINTMENT_ID "varchar2(36)" [not null]
  CREATED_AT "timestamp" [not null]
  MESSAGE "varchar2(1000)" [not null]
  SENT_AT "timestamp" [not null]
  STATUS "varchar2(20)" [not null]
  TYPE "varchar2(20)" [not null]
  UPDATED_AT "timestamp"
  USER_ID "varchar2(36)"
  CLIENT_ID "varchar2(36)"
  Note: 'Exactamente uno de USER_ID (personal) o CLIENT_ID (cliente) debe estar lleno (CHECK)'
}

Table DIAGNOSTICS {
  DIAGNOSTIC_ID "varchar2(36)" [pk]
  CODE "varchar2(15)" [not null]
  CREATED_AT "timestamp" [not null]
  DESCRIPTION "varchar2(500)"
  IS_ACTIVE "number(1)" [not null]
  NAME "varchar2(150)" [not null]
  UPDATED_AT "timestamp"
}

Table MEDICAL_RECORDS {
  RECORD_ID "varchar2(36)" [pk]
  APPOINTMENT_ID "varchar2(36)" [not null]
  CLIENT_PET_ID "varchar2(36)" [not null]
  CREATED_AT "timestamp" [not null]
  DIAGNOSTIC_ID "varchar2(36)" [not null]
  SYMPTOMS "varchar2(1000)"
  TEMPERATURE "number"
  TREATMENT "varchar2(1000)"
  UPDATED_AT "timestamp"
  WEIGHT_AT_VISIT "number"
}

Table VACCINATIONS {
  VACCINATION_ID "varchar2(36)" [pk]
  APPLICATION_DATE "date" [not null]
  CLIENT_PET_ID "varchar2(36)" [not null]
  CREATED_AT "timestamp" [not null]
  DOSE_NUMBER "number" [not null]
  NEXT_DOSE_DATE "date"
  RECORD_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
  VACCINE_NAME "varchar2(30)" [not null]
}

Table AGENT_HUMANS {
  AGENT_HUMAN_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  IS_ACTIVE "number(1)" [not null]
  UPDATED_AT "timestamp"
  USER_ID "varchar2(36)" [not null]
}

Table CHAT_CONVERSATIONS {
  CHAT_CONVERSATION_ID "varchar2(36)" [pk]
  CHANNEL "varchar2(20)" [not null]
  CREATED_AT "timestamp" [not null]
  LAST_MESSAGE_AT "timestamp"
  UPDATED_AT "timestamp"
}

Table CHAT_PARTICIPANTS {
  CHAT_PARTICIPANT_ID "varchar2(36)" [pk]
  AGENT_HUMAN_ID "varchar2(36)"
  CHAT_CONVERSATION_ID "varchar2(36)" [not null]
  CLIENT_ID "varchar2(36)"
  CREATED_AT "timestamp" [not null]
  SENDER_TYPE_ID "varchar2(36)" [not null]
  UPDATED_AT "timestamp"
  Note: 'Exactamente uno de CLIENT_ID o AGENT_HUMAN_ID debe estar lleno (CHECK)'
}

Table CHAT_MESSAGES {
  CHAT_MESSAGE_ID "varchar2(36)" [pk]
  CHAT_CONVERSATION_ID "varchar2(36)" [not null]
  CHAT_PARTICIPANT_ID "varchar2(36)" [not null]
  CONTENT "clob" [not null]
  CREATED_AT "timestamp" [not null]
  METADATA "clob"
  SENDER_TYPE_ID "varchar2(36)" [not null]
}

Table CHAT_ESCALATIONS {
  CHAT_ESCALATION_ID "varchar2(36)" [pk]
  CHAT_CONVERSATION_ID "varchar2(36)" [not null]
  CREATED_AT "timestamp" [not null]
  ESCALATION_STATUS_ID "varchar2(36)" [not null]
  FROM_AI "number(1)" [not null]
  REASON "clob"
  RESOLVED_AT "timestamp"
  RESOLVED_BY "varchar2(36)"
  RESOLUTION_NOTE "clob"
  UPDATED_AT "timestamp"
}

Table SENDER_TYPES {
  SENDER_TYPE_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  NAME_TYPE "varchar2(50)" [not null]
  UPDATED_AT "timestamp"
}

Table ESCALATIONS_STATUSES {
  ESCALATION_STATUS_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  NAME_STATUS "varchar2(50)" [not null]
  UPDATED_AT "timestamp"
}

Table TELEGRAM_USER_LINKS {
  TELEGRAM_USER_LINK_ID "varchar2(36)" [pk]
  CREATED_AT "timestamp" [not null]
  LINKED_AT "timestamp" [not null]
  CLIENT_ID "varchar2(36)" [not null]
  CHAT_CONVERSATION_ID "varchar2(36)"
  TELEGRAM_CHAT_ID "number(19)" [not null]
  TELEGRAM_USER_ID "number(19)" [not null]
  UNLINKED_AT "timestamp"
  UPDATED_AT "timestamp"
}

Table TELEGRAM_INBOUND_UPDATES {
  UPDATE_ID "number(19)" [pk]
  ATTEMPTS "number(10)" [not null]
  CHAT_TYPE "varchar2(30)" [not null]
  CREATED_AT "timestamp" [not null]
  LAST_ERROR_CODE "varchar2(120)"
  LAST_SENT_CHUNK_INDEX "number(10)" [not null]
  MESSAGE_TEXT "clob"
  NEXT_ATTEMPT_AT "timestamp" [not null]
  RESPONSE_TEXT "clob"
  STATUS "varchar2(20)" [not null]
  TELEGRAM_CHAT_ID "number(19)" [not null]
  TELEGRAM_MESSAGE_ID "number(19)" [not null]
  TELEGRAM_USER_ID "number(19)" [not null]
  UPDATED_AT "timestamp"
}

Table CONTACT_VERIFICATION_SESSIONS {
  ID "varchar2(36)" [pk]
  ATTEMPTS "number(10)" [not null]
  CHANNEL "varchar2(20)" [not null]
  CREATED_AT "timestamp" [not null]
  DESTINATION_HASH "varchar2(64)" [not null]
  EXPIRES_AT "timestamp"
  OTP_HASH "varchar2(64)"
  PROOF_EXPIRES_AT "timestamp"
  PROOF_HASH "varchar2(64)"
  PURPOSE "varchar2(20)" [not null]
  STATUS "varchar2(20)" [not null]
  SUBJECT_USER_ID "varchar2(36)" [note: 'guarda el id del CLIENTE (sin FK); pasa a SUBJECT_CLIENT_ID en el frente de renombres']
  UPDATED_AT "timestamp"
  Note: 'OTP por correo solo para reclamar un cliente existente desde otro Telegram (contrato v2, 9.1). Se conserva.'
}

// Relaciones
Ref: AGENT_HUMANS.USER_ID > USERS.USER_ID
Ref: APPOINTMENTS.AVAILABILITY_ID > AVAILABILITIES.AVAILABILITY_ID
Ref: APPOINTMENTS.CLIENT_PET_ID > CLIENTS_PETS.CLIENT_PET_ID
Ref: APPOINTMENTS.SERVICE_ID > SERVICES.SERVICE_ID
Ref: APPOINTMENTS.STATUS_APPOINTMENT_ID > STATUS_APPOINTMENTS.STATUS_APPOINTMENT_ID
Ref: APPOINTMENTS.VETERINARIAN_ID > VETERINARIANS.VETERINARIAN_ID
Ref: APPOINTMENT_STATUS_HISTORIES.APPOINTMENT_ID > APPOINTMENTS.APPOINTMENT_ID
Ref: APPOINTMENT_STATUS_HISTORIES.CLIENT_PET_ID > CLIENTS_PETS.CLIENT_PET_ID
Ref: APPOINTMENT_STATUS_HISTORIES.STATUS_APPOINTMENT_ID > STATUS_APPOINTMENTS.STATUS_APPOINTMENT_ID
Ref: AVAILABILITIES.VETERINARIAN_ID > VETERINARIANS.VETERINARIAN_ID
Ref: CHAT_ESCALATIONS.CHAT_CONVERSATION_ID > CHAT_CONVERSATIONS.CHAT_CONVERSATION_ID
Ref: CHAT_ESCALATIONS.ESCALATION_STATUS_ID > ESCALATIONS_STATUSES.ESCALATION_STATUS_ID
Ref: CHAT_ESCALATIONS.RESOLVED_BY > USERS.USER_ID
Ref: CHAT_MESSAGES.CHAT_CONVERSATION_ID > CHAT_CONVERSATIONS.CHAT_CONVERSATION_ID
Ref: CHAT_MESSAGES.CHAT_PARTICIPANT_ID > CHAT_PARTICIPANTS.CHAT_PARTICIPANT_ID
Ref: CHAT_MESSAGES.SENDER_TYPE_ID > SENDER_TYPES.SENDER_TYPE_ID
Ref: CHAT_PARTICIPANTS.AGENT_HUMAN_ID > AGENT_HUMANS.AGENT_HUMAN_ID
Ref: CHAT_PARTICIPANTS.CHAT_CONVERSATION_ID > CHAT_CONVERSATIONS.CHAT_CONVERSATION_ID
Ref: CHAT_PARTICIPANTS.CLIENT_ID > CLIENTS.CLIENT_ID
Ref: CHAT_PARTICIPANTS.SENDER_TYPE_ID > SENDER_TYPES.SENDER_TYPE_ID
Ref: CLIENTS_PETS.CLIENT_ID > CLIENTS.CLIENT_ID
Ref: CLIENTS_PETS.PET_ID > PETS.PET_ID
Ref: MEDICAL_RECORDS.APPOINTMENT_ID > APPOINTMENTS.APPOINTMENT_ID
Ref: MEDICAL_RECORDS.CLIENT_PET_ID > CLIENTS_PETS.CLIENT_PET_ID
Ref: MEDICAL_RECORDS.DIAGNOSTIC_ID > DIAGNOSTICS.DIAGNOSTIC_ID
Ref: NOTIFICATIONS.APPOINTMENT_ID > APPOINTMENTS.APPOINTMENT_ID
Ref: NOTIFICATIONS.CLIENT_ID > CLIENTS.CLIENT_ID
Ref: NOTIFICATIONS.USER_ID > USERS.USER_ID
Ref: PETS.RACE_ID > RACES.RACE_ID
Ref: PETS.SPECIES_ID > SPECIES.SPECIES_ID
Ref: RACES.SPECIES_ID > SPECIES.SPECIES_ID
Ref: ROLE_PERMISSIONS.MODULE_ID > MODULES.MODULE_ID
Ref: ROLE_PERMISSIONS.ROLE_ID > ROLES.ROLE_ID
Ref: SERVICES.TYPE_SERVICE_ID > TYPE_SERVICES.TYPE_SERVICE_ID
Ref: TELEGRAM_USER_LINKS.CHAT_CONVERSATION_ID > CHAT_CONVERSATIONS.CHAT_CONVERSATION_ID
Ref: TELEGRAM_USER_LINKS.CLIENT_ID > CLIENTS.CLIENT_ID
Ref: USERS.ROLE_ID > ROLES.ROLE_ID
Ref: USER_TOKENS.USER_ID > USERS.USER_ID
Ref: VACCINATIONS.CLIENT_PET_ID > CLIENTS_PETS.CLIENT_PET_ID
Ref: VACCINATIONS.RECORD_ID > MEDICAL_RECORDS.RECORD_ID
Ref: VETERINARIANS.SPECIALTY_ID > SPECIALTIES.SPECIALTY_ID
Ref: VETERINARIANS.USER_ID > USERS.USER_ID
Ref: VETERINARIAN_ABSENCES.VETERINARIAN_ID > VETERINARIANS.VETERINARIAN_ID

TableGroup Usuarios_y_seguridad {
  USERS
  USER_TOKENS
  ROLES
  ROLE_PERMISSIONS
  MODULES
}

TableGroup Clientes_y_mascotas {
  CLIENTS
  CLIENTS_PETS
  PETS
  SPECIES
  RACES
}

TableGroup Personal_clinico {
  VETERINARIANS
  SPECIALTIES
  AVAILABILITIES
  VETERINARIAN_ABSENCES
}

TableGroup Servicios_y_citas {
  TYPE_SERVICES
  SERVICES
  STATUS_APPOINTMENTS
  APPOINTMENTS
  APPOINTMENT_STATUS_HISTORIES
  NOTIFICATIONS
}

TableGroup Historia_clinica {
  DIAGNOSTICS
  MEDICAL_RECORDS
  VACCINATIONS
}

TableGroup Chat_y_escalamiento {
  AGENT_HUMANS
  CHAT_CONVERSATIONS
  CHAT_PARTICIPANTS
  CHAT_MESSAGES
  CHAT_ESCALATIONS
  SENDER_TYPES
  ESCALATIONS_STATUSES
}

TableGroup Telegram {
  TELEGRAM_USER_LINKS
  TELEGRAM_INBOUND_UPDATES
  CONTACT_VERIFICATION_SESSIONS
}

```
