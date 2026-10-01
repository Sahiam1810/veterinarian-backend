# Contrato de API v3 — fusión de usuarios y permisos por rol

Estado: **borrador para el Frente 2** (backend, frontend y chatbot). Se ejecuta **después** del Frente 1 (separación de clientes y usuarios; ver [`clientes-usuarios-api-v2.md`](clientes-usuarios-api-v2.md)).
Alcance: solo lo que cambia por fusionar `USER_ACCOUNTS` y `USER_CREDENTIALS` dentro de `USERS` y por eliminar los permisos por usuario.

## 0. Cómo usar este documento

Es la **fuente de verdad** del Frente 2. Si el código actual lo contradice, gana este documento. Las reglas de la sección 0 del contrato v2 aplican igual: trabaja solo tu tarea, **no generes migraciones**, no renombres tablas ni columnas por tu cuenta, no agregues compatibilidad hacia atrás y deja los tests de tu parte en verde.

## 1. Contexto

Hoy un usuario del personal son **tres filas atadas**: `USERS` (nombre, correo, rol, estado), `USER_ACCOUNTS` (nombre de usuario, correo repetido, estado como texto) y `USER_CREDENTIALS` (contraseña). El login busca por el correo de la **cuenta**, y el token lleva como `sub` el id de la **cuenta**, no el del usuario.

Después del Frente 2:

- `USERS` guarda todo: nombre, **correo (único, es el login)**, contraseña, rol, `IS_ACTIVE`, foto, fechas.
- No existe `USERNAME`: se inicia sesión **solo con el correo**.
- `USER_TOKENS` (refresh tokens) apunta a `USERS`.
- No hay permisos por usuario: **los permisos salen solo del rol**.
- Desaparecen `ACCOUNT_STATEMENTS` y los flujos viejos de Telegram por código y por registro web.

## 2. Token JWT

| Claim | Antes (personal) | Después (personal) |
|---|---|---|
| `sub` | id de la **cuenta** | id del **usuario** |
| `person_id` | id del usuario | **se elimina** |
| `role_id`, `role` | igual | igual |
| `preferred_username` | nombre de usuario | **correo** (se conserva la claim) |
| `email` | correo de la cuenta | correo del usuario |
| `permissions` | rol + excepciones por usuario | **solo rol** |

- **Cliente delegado (bot):** ya vale `sub` = id del cliente (Frente 1). Solo se elimina `person_id`.
- **Chatbot:** deja de exigir `person_id`; `userId` del mensaje debe ser igual a `sub`.
- **Sesiones:** al desplegar, los tokens anteriores dejan de servir (el `sub` cambia de significado y los refresh tokens se recrean). Todos vuelven a iniciar sesión.
- El resto de claims estándar (`iss`, `aud`, `jti`, `iat`, `nbf`, `exp`) y la firma no cambian.

## 3. Endpoints

**Se eliminan** (todas sus rutas):

- `/api/UserAccounts/*`
- `/api/UserCredentials/*` (incluye `PATCH /api/UserCredentials/{id}/change-password`; el cambio de contraseña propio sigue siendo `PATCH /api/auth/me/password`)
- `/api/user-permissions/*`
- `/api/AccountStatements/*`
- `POST /api/integrations/telegram/link-codes`
- `GET` y `POST /telegram/registration/complete`

**Cambian:**

| Endpoint | Cambio |
|---|---|
| `POST /api/users` | Un solo paso: `{ fullName, email, password, roleId, specialtyId?, licenseNumber? }` → deja al usuario listo para iniciar sesión. Ya no hay que crear cuenta ni credenciales aparte |
| `PATCH /api/users/{id}/activate` y `/deactivate` (rutas actuales) | Cambian solo `IS_ACTIVE` y borran los refresh tokens del usuario. Sin cuenta |
| `GET /api/auth/me` | Respuesta `{ id, fullName, initials, email, role, photoUrl }`. **Desaparecen** `personId`, `userAccountId`, `userName` y `accountStatus` |
| `POST /api/auth/login` | Sin cambio de forma. Busca por `USERS.EMAIL` (sin distinguir mayúsculas) |
| `POST /api/auth/refresh`, `POST /api/auth/revoke` | Sin cambio de forma. Los refresh tokens pertenecen al usuario |
| `GET /api/auth/permissions` | Sin cambio de forma. Solo refleja el rol |

Los códigos de error de autenticación (`Authentication.*`) **no cambian**. Se eliminan los de cuentas y credenciales (`UserAccounts.*`, `UserCredentials.*`); hay que actualizar `docs/contracts/api-error-codes-catalog.md`.

## 4. Decisiones tomadas

1. Login solo por correo; `USERNAME` desaparece.
2. Sin excepciones de permisos por usuario. SuperAdmin conserva su tratamiento especial por rol.
3. `preferred_username` se conserva con el correo como valor, para no tocar el chatbot ni el frontend por esa claim.
4. `IS_ACTIVE` reemplaza al estado de la cuenta (`Activo` / `Inactivo`). La migración calcula `IS_ACTIVE` = usuario activo **y** cuenta `Activo`.
5. La contraseña vigente es la de `USER_CREDENTIALS`; la migración la copia a `USERS.PASSWORD_HASH` antes de borrar la tabla.
6. El correo vigente es el de `USER_ACCOUNTS.MAIL` (el del login), no el de `USERS.EMAIL`, si difieren.
7. `ACCOUNT_STATEMENTS` y los flujos de Telegram por código y por registro web se eliminan en este frente porque dependen de `USER_ACCOUNTS`.
