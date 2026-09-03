# Contexto de revisión — Backend Huellitas

**Propósito:** que Gallo, Tomás y Sahiam puedan revisar el backend en paralelo, cada uno en sus módulos asignados, sin preguntar contexto adicional y sin duplicar trabajo ya hecho.

**Estado del repo a la fecha de este documento:** `develop` @ `5a101a5` (2026-09-01) — incluye la auditoría de Auth (`365d7c5`, `45e840f`) y el fix de `RolesController` (`17089fa`, PR #69), ambos ya comiteados. Además, el mismo día se auditó y corrigió Users/UserAccounts/UserTokens (SEC-03 + P1/P2, ver §3) — ese trabajo está **sin commitear** todavía, incluida una migración de EF (`AddUserAccountsMailUniqueIndex`) generada pero sin aplicar contra la base compartida. Build y `dotnet test` en verde: **465/465**.

**Cómo se armó este documento:** no es un resumen de memoria — cada afirmación de las secciones 2, 3 y 4 se verificó releyendo el archivo correspondiente o el commit correspondiente el mismo día que se escribió esto. Si algo cambia después de este commit, ese cambio **no** está reflejado aquí — corre `git log` sobre los archivos que te toquen antes de asumir que esto sigue vigente. Auth, `RolesController` y Users son la excepción: quedaron auditados y cerrados hoy (§2, §3, §4), no los vuelvan a revisar salvo que toquen esos archivos.

---

## 0. Alcance de producto y estado del frontend (leer primero, actualizado 2026-09-03)

**El Cliente nunca tiene interfaz propia ni login.** Se había planeado en algún momento un panel de cliente (self-service web/app con JWT, viendo sus citas/mascotas/etc.) — **esa idea se descartó**, indicación explícita de la líder: el Cliente **solo** interactúa con el sistema a través del chatbot (Telegram por ahora). Todo lo que el cliente necesita — agendar cita, cancelar, reprogramar, consultar sus mascotas — pasa por el chatbot, no por un frontend propio del cliente.

**Qué implica esto para el backend:**
- Todo el trabajo ya hecho para Cliente (`ClientOnly`, `/clients/me`, `/pets/mine`, `/appointments/mine`, el flujo OTP de auto-servicio de citas sin JWT, etc.) **se deja tal cual está, quieto** — no se retira ni se completa activamente. Queda catalogado como **mejora futura**, no como pendiente de esta ronda. No reportar como "hallazgo" el hecho de que el panel de Cliente esté incompleto o inconsistente con el resto — es simplemente un camino que no se va a seguir desarrollando por ahora.
- **No tocar nada de lo que es exclusivamente de Cliente** (rutas `ClientOnly`, controllers `/mine`) salvo que el hallazgo sea de seguridad real explotable por otro rol, o que se pida explícitamente.
- El chatbot (Telegram + subsistemas de Chat/Escalamientos/IA-Agente, ver §6) es, en cambio, el canal real y activo del Cliente — ahí sí aplica todo el peso de la revisión y corrección.

**Estado del frontend (para contexto, no accionable desde el backend):** SuperAdmin, Veterinario y Auxiliar ya están **100% conectados** al frontend real (no es solo backend con Swagger — hay UI consumiéndolos en producción/staging). Tenerlo en cuenta al estimar impacto de un cambio: romper un contrato de esos tres roles es visible para usuarios reales ahora mismo, no solo teórico.

---

## 1. Arquitectura y patrones establecidos

No propongas nada distinto a esto sin discutirlo antes — son decisiones ya tomadas y aplicadas en la mayoría del código.

### Capas
Clean Architecture: `Domain` → `Application` → `Infrastructure` ← `Api`. CQRS con MediatR (un `Command`/`Query` + su `Handler` por caso de uso, en `Application/<Módulo>/UseCases/`). Persistencia vía patrón Repository (`I<Entidad>Repository` en `Application/<Módulo>/Abstraction/`, implementación en `Infrastructure/<Módulo>/Repositories/`) agregados detrás de un único `IUnitOfWork` (`Application/Common/Abstractions/IUnitOfWork.cs`). **Todo repositorio nuevo se registra en 3 sitios**: `IUnitOfWork.cs`, `Infrastructure/UnitOfWork/UnitOfWork.cs` y `Infrastructure/DependencyInjection.cs` — si falta uno de los tres no compila.

### Sistema de autorización — `[RequirePermission]`
La forma correcta de proteger un endpoint hoy es:

```csharp
[RequirePermission("NombreDelModulo", PermissionAction.View)]   // o Create / Edit / Delete
```

- `RequirePermission` (`Api/Common/Security/Permissions/RequirePermissionAttribute.cs`) arma una policy dinámica `"perm:{módulo}:{acción}"`, resuelta al vuelo por `PermissionPolicyProvider` (no hay que registrar una policy por combinación).
- `PermissionAuthorizationHandler` (`Api/Common/Security/Permissions/PermissionAuthorizationHandler.cs`) es quien decide: primero revisa el claim `super_admin=true` del JWT (si está, aprueba sin consultar nada más — el SuperAdmin se salta *todo* el sistema de permisos). Si no es SuperAdmin, lee `role_id` y `person_id` del JWT y llama `GetEffectivePermissionQuery`.
- `GetEffectivePermissionQueryHandler` (`Application/Permissions/UseCases/`) combina **`RolePermission`** (permiso del rol) **OR `UserPermission`** (permiso puntual del usuario) por cada acción — es **aditivo**: `UserPermission` solo puede sumar, nunca quitar lo que ya da el rol. Si `USER_PERMISSIONS` está vacía, el sistema se comporta exactamente como si solo existiera `RolePermission` (verificado con tests unitarios en `GetEffectivePermissionQueryHandlerTests`).
- El nombre del módulo en el atributo debe **coincidir exactamente** (case-sensitive, con tildes) con una fila en la tabla `MODULES`. Si no existe esa fila, el endpoint queda inaccesible para todo el mundo excepto SuperAdmin — así se rompió `RolesController` hasta hoy (ver §3, ya corregido).
- El propio usuario autenticado puede ver sus permisos efectivos vía `GET /api/auth/permissions` (agregado hoy, ver sección 3).
- **Catálogo actual de módulos** (17 filas en `MODULES`, verificado en vivo): Clientes, Mascotas, Especies y Razas, Especialidades, Veterinarios, Citas, Historiales Clínicos, Servicios, Estados de Cita, Cuentas y Pagos, Notificaciones, Usuarios, **Roles** (agregado hoy, ver §3), Chat, Escalamientos, IA y Agente, Catálogos del Chat. **"Roles y Permisos" sigue sin existir como módulo propio** — la gestión de permisos (`RolePermissionsController`/`UserPermissionsController`/escritura de `ModulesController`) es intencionalmente `SuperAdminOnly` y no pasa por `RequirePermission`, así que no necesita una fila en `MODULES`.
- Los 3 controllers de gestión de permisos (`ModulesController` escritura, `RolePermissionsController` completo, `UserPermissionsController` completo) están protegidos con `[Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]` (`RequireClaim("super_admin","true")`) — **no** pasan por `RequirePermission`, es intencional: la gestión de roles/permisos es exclusiva de SuperAdmin y no se puede delegar ni siquiera vía `UserPermission`.
- `GET /api/auth/permissions` refleja el mismo bypass: si el JWT trae `super_admin=true` (no tiene `role_id`), el endpoint no consulta la matriz — devuelve los 4 flags en `true` para todos los módulos de `MODULES` directamente (antes daba 401 porque intentaba parsear un `role_id` que el SuperAdmin no tiene).

### Rate limiting
Login/Register/Refresh/TelegramWebhook usan `[EnableRateLimiting(RateLimitPolicies.<Policy>)]` + la policy correspondiente registrada por `AddApiRateLimiting` (`Api/Extensions/RateLimitingExtensions.cs`), particionada por claim `sub` si hay usuario autenticado o por IP si no. Los límites (permit limit + ventana en segundos, más un `GlobalPermitLimit` que aplica a toda la API) viven en la sección `"RateLimiting"` de `appsettings.json`, con `RateLimitOptionsValidator` exigiendo que todos sean positivos (`ValidateOnStart`). **Hasta el 2026-09-01 esta implementación existía en el código pero `Program.cs` nunca la invocaba** — usaba en su lugar un bloque `AddRateLimiter`/`AddFixedWindowLimiter` hardcodeado y **sin partición** (un único contador compartido por todos los clientes de la API para cada policy), lo que además de ser más débil contra fuerza bruta permitía que cualquiera agotara el login de todo el mundo con 10 requests. Ya está corregido y conectado — no lo reporten de nuevo.

### Formato de error canónico
Toda excepción no controlada la captura `GlobalExceptionHandler` (`Api/Common/Errors/GlobalExceptionHandler.cs`) y la traduce a un `ApiErrorResponse` (`Api/Common/Errors/ApiErrorResponse.cs`: `Timestamp, Status, Error, Message, Path, Violations`). Mapeo actual:

| Excepción (`Application/Common/Exceptions/`) | HTTP |
|---|---|
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `UnauthorizedException` | 401 |
| `BadRequestException` | 400 |
| `ValidationException` (FluentValidation) | 400, con `Violations` por campo |
| `DbUpdateException` (EF) | 409 |
| Cualquier otra | 500 |

**Patrón correcto**: el *handler* de Application lanza la excepción (`?? throw new NotFoundException(...)`); el *controller* nunca hace `is null ? NotFound() : Ok()` a mano — simplemente llama `sender.Send(...)` y envuelve el resultado en `Ok(...)`/`NoContent()`. Un refactor grande el 2026-09-01 (commit `6cd7068`) migró 18 controllers de un patrón viejo (`Handler` devolvía `bool`/`T?`, controller decidía 404 a mano) a este patrón nuevo — si ves un controller con `is null ? NotFound() : Ok(...)` o un `Handler` que retorna `bool`, es candidato a limpieza con este mismo patrón, pero **repórtalo, no lo cambies tú si el módulo no es tuyo** (ver sección 5).

### Patrón "ver solo lo propio" (`/mine`)
Ya existen `GET /api/clients/me`, `GET /api/pets/mine`, `GET /api/appointments/mine` — todos resuelven la identidad desde el JWT (`sub`/`NameIdentifier` → `UserAccountsRepository` → `ClientsRepository.GetByUserIdAsync` → `ClientPetsRepository.GetByClientIdAsync`) y devuelven solo lo del cliente autenticado.

**Por qué el `GetAll` general (`GET /api/pets`, `GET /api/clientspets`, etc.) no filtra por dueño**: esos endpoints son para el personal (Admin/Vet/Recepcionista/Auxiliar) y devuelven todo sin filtrar a propósito — el filtrado por dueño vive en el endpoint `/mine` separado. Migrar el `GetAll` general al mismo permiso que le da acceso a Cliente (`"Mascotas": View`) sin agregar filtrado real sería un hueco de privacidad — por eso, en varios módulos (Pets, ClientsPets, Appointments, AppointmentStatusHistories, Availabilities, AccountStatements), el `GetAll`/`GetById` general se dejó deliberadamente en la policy vieja (`StaffOnly`, basada en rol) en vez de migrarlo a `RequirePermission`, porque Cliente nunca estuvo en `StaffOnly` — así no gana acceso sin querer. Ver el detalle exacto por controller en la sección 2.

**Excepción importante — `MedicalRecords`/`Vaccinations`**: ahí el `GetAll`/`GetById` general **sí** está en `RequirePermission`, pero el *handler* (no el controller) hace el filtrado: si el `UserAccountId` resuelve a un `Client`, filtra por sus `ClientPetId`; si no (personal), devuelve todo. Ver `GetAllMedicalRecordsQueryHandler`/`GetAllVaccinationsQueryHandler` como referencia si necesitas replicar este patrón en otro módulo.

---

## 2. Estado real por controller (no-chatbot)

26 controllers de módulo + `AuthController`. Verificado línea por línea el 2026-09-01. **Todos** tienen Domain+EF+Migración aplicada, Repo+UoW registrado, CQRS con MediatR, y Swagger (`EndpointSummary`/`EndpointDescription`) — no se repite esa columna porque es uniforme; solo se marca cuando **no** es así.

| Controller | Endpoints y autorización actual | Notas |
|---|---|---|
| `AccountStatementsController` | POST→`Cuentas y Pagos:Create`, GET/{id}→`StaffOnly` (deliberado), GET by-account→`StaffOnly` (deliberado), PATCH status→`Cuentas y Pagos:Edit`, DELETE→`Cuentas y Pagos:Delete` | GetById/by-account sin migrar a propósito (ver §1) |
| `AppointmentStatusHistoriesController` | POST→`Citas:Edit`, GET→`StaffOnly` (deliberado), GET/{id}→`StaffOnly` (deliberado), PUT→`Citas:Edit`, DELETE→`Citas:Delete` | POST usa `Edit` no `Create` — "cambiar estado" se modeló como editar la cita, para que Veterinario (V+E, sin C) pueda usarlo. 🟢 **Auditado y cerrado 2026-09-03** — bypass de las reglas de transición de estado corregido, ver VET-04/05 en §4 y §3 |
| `AppointmentsController` | GET mine→`ClinicalHistoryReadOnly`, POST→`Citas:Create`, GET→`StaffOnly` (deliberado), GET/{id}→`StaffOnly` (deliberado), PUT→`Citas:Edit`, DELETE→`Citas:Delete` | **`/mine` tiene el bug SEC-01 activo hoy** — ver §3 y §4 |
| `AvailabilitiesController` | POST→`Citas:Create`, GET→`StaffOnly` (deliberado), GET/{id}→`StaffOnly` (deliberado), GET by-veterinarian→`StaffOnly` (deliberado), PUT→`Citas:Edit`, DELETE→`Citas:Delete` | |
| `ClientsController` | GET me→`ClientOnly`, GET→`Clientes:View`, GET/{id}→`Clientes:View`, POST→`Clientes:Create`, PUT→`Clientes:Edit`, DELETE→`Clientes:Delete` | Migración completa, sin excepciones |
| `ClientsPetsController` | GET→`StaffOnly` (deliberado), GET/{id}→`StaffOnly` (deliberado), POST→`Mascotas:Create`, PUT→`Mascotas:Edit`, DELETE→`Mascotas:Delete` | Mapeado al módulo "Mascotas", no "Clientes" — fue decisión propia, no aprobada explícitamente por la líder, confirmar si les hace sentido |
| `DiagnosticsController` | GET→`Historiales Clínicos:View`, GET/{id}→`Historiales Clínicos:View`, POST→`...Create`, PUT→`...Edit`, DELETE→`...Delete` | Migración completa (antes tenía auth mixta, ya no) |
| `MedicalRecordsController` | POST→`Historiales Clínicos:Create`, GET→`...View` (filtrado por dueño en el handler), GET/{id}→`...View` (ídem) | Sin PUT/DELETE — el modelo asume historia clínica inmutable |
| `ModulesController` | GET→`StaffOnly`, GET/{id}→`StaffOnly`, POST/PUT/DELETE→`SuperAdminOnly` | |
| `NotificationsController` | POST→`Notificaciones:Create`, GET/{id}/user/{id}/appointment/{id}→`...View`, PUT→`...Edit`, DELETE→`...Delete` | Matriz solo da V+D a Administrador — POST/PUT quedan sin ningún rol que pase (intencional, ver §3) |
| `PetsController` | GET mine→`ClientOnly`, GET→`StaffOnly` (deliberado), GET/{id}→`StaffOnly` (deliberado), POST→`Mascotas:Create`, PUT→`Mascotas:Edit`, DELETE→`Mascotas:Delete` | |
| `RacesController` | Los 5 endpoints→`Especies y Razas:<acción>` | Migración completa |
| `RolePermissionsController` | Los 6 endpoints→`SuperAdminOnly` | |
| `RolesController` | Los 5 endpoints→`RequirePermission("Roles", ...)` | 🟢 **Corregido 2026-09-01** — módulo "Roles" creado, Administrador con V C E D, ningún otro rol tiene fila (403). Probado en vivo, ver §3. |
| `ServicesController` | Los 5 endpoints→`Servicios:<acción>` | Comparte módulo "Servicios" con `TypeServicesController` |
| `SpecialtiesController` | Los 5 endpoints→`Especialidades:<acción>` | |
| `SpeciesController` | Los 5 endpoints→`Especies y Razas:<acción>` | |
| `StatusAppointmentsController` | Los 5 endpoints→`Estados de Cita:<acción>` | |
| `TypeServicesController` | Los 5 endpoints→`Servicios:<acción>` | |
| `UserAccountsController` | Los 5 endpoints→`Usuarios:<acción>` | 🟢 **Auditado y cerrado 2026-09-01** — `Mail` ahora valida duplicado (409, mismo patrón que `Username`) e índice único real en BD (migración `AddUserAccountsMailUniqueIndex`); `Status` restringido a `Activo`/`Inactivo` (`Domain.UserAccounts.ValueObjects.AccountStatus`), ya no texto libre. Ver §3 |
| `UserCredentialsController` | POST→`Usuarios:Create`, GET/{id}/by-account→`...View`, PATCH change-password→`SuperAdminOnly` | SEC-02 implementado 2026-09-01: reset de contraseña ajena exclusivo de SuperAdmin (ya no `Usuarios:Edit`); autoservicio movido a `PATCH /api/auth/me/password` |
| `UserPermissionsController` | Los 6 endpoints→`SuperAdminOnly` | |
| `UserTokensController` | Los 4 endpoints→`SuperAdminOnly` | 🟢 **Auditado y cerrado 2026-09-01** — antes `RequirePermission("Usuarios", ...)`, lo que junto con la creación manual sin restricciones permitía forjar un refresh token válido para cualquier cuenta (ver SEC-03 en §3). Ahora exclusivo de SuperAdmin + el validator de creación rechaza `TokenType: "refresh"`/`"access"` |
| `UsersController` | POST→`Create`, GET/GET{id}→`View`, PUT/deactivate/activate→`Edit` | 🟢 **Auditado y cerrado 2026-09-01** — `deactivate`/`activate` ahora sí revocan/restauran acceso real (ver SEC-03 en §3); `POST` documenta en Swagger que hace falta además `POST /api/useraccounts` + `POST /api/usercredentials` para que el usuario pueda loguearse |
| `VaccinationsController` | POST→`Historiales Clínicos:Create`, GET/GET{id}→`...View` (filtrado por dueño en el handler), PUT→`...Edit` | Sin DELETE |
| `VeterinariansController` | Los 5 endpoints→`Veterinarios:<acción>` | Sin `GET /me` — ver VET-02/03 pendiente |
| `AuthController` | register (anónimo, ahora exige `IdentificationNumber` y crea el `Client`)/login/refresh (anónimo), `GET me` (self), `GET permissions` (self, incluye SuperAdmin), **`PATCH me/password`** (self, nuevo — SEC-02), revoke | No es un módulo CRUD, es infraestructura de auth. 🟢 **Auditado completo 2026-09-01 (Domain/Application/Infrastructure/Api) — cerrado, ver §3.** |

---

## 3. Historial de correcciones recientes ya aplicadas

No las reporten de nuevo. Orden cronológico, con autor real de `git log` (no asumido):

| Fecha | Commit | Qué se hizo | Autor |
|---|---|---|---|
| 2026-08-31 | `8c280ea` | SuperAdmin: policy `SuperAdminOnly`, `SuperAdminOptions` (config, no fila en DB), login especial, `/me` sintético | Sahiam1810 |
| 2026-08-31/09-01 | `894d110` | Tabla `USER_PERMISSIONS`, `UserPermissionsRepository`, tests de `PermissionAuthorizationHandler` y `GetEffectivePermissionQueryHandler` | Sahiam1810 |
| 2026-09-01 | `02afedd` | Migración masiva a `RequirePermission` de Species/StatusAppointments/TypeServices/UserAccounts/UserCredentials/UserTokens/Users/Vaccinations/Veterinarians + seed inicial de `ROLE_PERMISSIONS` (47 filas, 5 roles × 12 módulos) | Sahiam1810 |
| 2026-09-01 | `7c0225c` | MedicalRecords/Vaccinations: filtrado por dueño en `GetAll`/`GetById` cuando el usuario tiene perfil de Cliente | Sahiam1810 |
| 2026-09-01 | `6cd7068` | Refactor de 18 controllers: de `Handler` devolviendo `bool`/`T?` + `is null ? NotFound() : Ok()` en el controller, a `Handler` lanzando `NotFoundException` (patrón canónico, ver §1) | Sahiam1810 |
| 2026-09-01 | `9fc66ff` | ⚠️ `GetMyAppointmentsQueryHandler`: cambió de `return Array.Empty<Appointment>()` a `return GetAllAsync()` cuando no hay perfil de Cliente — esto es lo que hoy causa **SEC-01**, ver §4 | Ksanti-monsalve |
| 2026-09-01 | `82dbf04` | `RequirePermission` en Diagnostics (completo) y **Roles** — este último quedó roto porque no se creó el módulo correspondiente; corregido hoy mismo, ver la fila de abajo | (verificar autor, no capturado en la muestra revisada) |
| 2026-09-01 | `a0a4afa` | Nuevo `GET /api/auth/permissions`, expone `GetEffectivePermissionQuery` (como `GetUserEffectivePermissionsQuery`, todos los módulos a la vez) al usuario autenticado | spostre |
| 2026-09-01 | `365d7c5`, `45e840f` | **Auditoría completa de Auth** (Domain/Application/Infrastructure/Api), todo verificado línea por línea y con tests nuevos: (1) **SEC-02** implementado — `PATCH /api/usercredentials/{id}/change-password` ahora `SuperAdminOnly`, nuevo `PATCH /api/auth/me/password` de autoservicio para cualquier rol; (2) `POST /api/auth/register` exige `IdentificationNumber` (misma regla que `CreateClientCommandValidator`) y crea el `Client` dentro de la misma transacción que `Users`/`UserAccounts`/`UserCredentials` — antes el usuario auto-registrado quedaba sin perfil de cliente y `/clients/me`, `/pets/mine`, `/appointments/mine` le daban 404 para siempre; (3) rate limiting de Login/Register/Refresh/TelegramWebhook reconectado: `Program.cs` usaba un bloque hardcodeado y **sin partición** (contador global compartido por todos los clientes), ahora usa `AddApiRateLimiting`/`UseApiRateLimiting`, particionado por usuario/IP y configurable vía `appsettings.json` (sección `RateLimiting`, antes inexistente); (4) `GET /api/auth/permissions` ya no da 401 a un SuperAdmin autenticado, devuelve los 4 flags en `true` para todos los módulos; (5) limpieza: `AuthenticationErrors.ForbiddenTokenOwner` y `RefreshTokenReuse` (ninguna se usaba en ningún lado) y la rama `Forbid()` en `AuthController.Revoke` eran código muerto (`RevokeAsync` nunca distingue ese caso) — eliminados; `UserTokens.IsExpired` pasó de `DateTime.UtcNow` directo a `IsExpiredAsOf(TimeProvider)`, con `AuthenticationService.RefreshAsync` usando el `TimeProvider` ya inyectado. | Sahiam1810 |
| 2026-09-01 | `17089fa` (PR #69) | **Fix del P0 de `RolesController`**: creado el módulo "Roles" (fila 17 en `MODULES`) vía `POST /api/modules` logueado como SuperAdmin — mismo mecanismo que los 16 módulos anteriores, ver el comentario en `database/seeds/role_permissions_seed.sql`. `ROLE_PERMISSIONS` actualizado (Administrador: V C E D sobre "Roles"; Veterinario/Recepcionista/Auxiliar/Cliente sin fila, igual que otros módulos administrativos). Verificado en vivo contra la base Oracle local: un usuario Administrador real pudo crear/listar/editar/eliminar un rol (201/200/204/204); un usuario Veterinario real recibió 403 en `POST`/`GET /api/roles`; y el Administrador siguió recibiendo 403 en `GET /api/role-permissions`, `GET /api/user-permissions` y `POST /api/modules` — confirma que este fix no le da a Administrador ningún acceso a la gestión de permisos, que sigue siendo exclusiva de SuperAdmin. No requirió cambios de código (la autorización de `RolesController` ya estaba bien escrita — el bug era solo la fila faltante en `MODULES`), por eso `dotnet test` no ganó tests nuevos por este ítem. | Sahiam1810 |
| 2026-09-01 | *(sin commit)* | **Auditoría completa de Users** (`UsersController`/`UserAccountsController`/`UserTokensController`; `UserCredentials` ya estaba cubierto por Auth) y fix de los hallazgos, todos con tests nuevos (antes no existía ningún test para estos tres controllers): **SEC-03** — dos P0 encontrados y cerrados juntos: (1) *forja de refresh tokens*: `POST /api/usertokens` persistía el `TokenValue` recibido tal cual, sin restringir `TokenType`; como `RefreshAsync` compara por igualdad exacta de string contra el hash, cualquiera con `Usuarios:Create` (hoy Administrador, sin ser SuperAdmin) podía precalcular `SHA256(secreto)`, mandarlo como `TokenValue` con `TokenType: "refresh"` apuntando a la cuenta de cualquier víctima, y usar ese `secreto` en `POST /api/auth/refresh` para obtener tokens válidos de esa cuenta sin su contraseña — eludía por completo la restricción de SEC-02. Cerrado con dos medidas juntas: `CreateUserTokenCommandValidator` rechaza `TokenType: "refresh"`/`"access"` (400), y `UserTokensController` completo pasó a `SuperAdminOnly` para los tipos que sí quedan permitidos (ej. `reset_password`); (2) *`deactivate` no revocaba nada real*: el Swagger del endpoint prometía "revocando su acceso al sistema", pero `DeactivateUserCommandHandler` solo tocaba `Users.IsActive`, un campo que `LoginAsync`/`RefreshAsync` nunca leen (validan `UserAccounts.Status`) — el usuario "desactivado" seguía logueándose y sus refresh tokens seguían siendo válidos. Ahora `Deactivate` marca también `UserAccounts.Status = "Inactivo"` y borra todos los `UserTokens` de esa cuenta (reutiliza el patrón de `RevokeAsync`); `Activate` restaura `UserAccounts.Status = "Activo"`. Además, dos P2: `UserAccounts.Mail` no tenía chequeo de duplicados (a diferencia de `Username`) — agregado `ExistsByMailAsync`, validado en ambos handlers (409) e índice único real en BD (migración `AddUserAccountsMailUniqueIndex`, **pendiente de aplicar** — revisar que no haya mails duplicados en datos existentes antes de correrla); `UserAccounts.Status` era texto libre — restringido a `Activo`/`Inactivo` (`Domain.UserAccounts.ValueObjects.AccountStatus`) en ambos validators. Y un P1: `POST /api/users` documenta ahora en Swagger que por sí solo no deja al usuario en condiciones de loguearse — hace falta además `POST /api/useraccounts` + `POST /api/usercredentials`, en ese orden (`Users.PasswordHash` nunca se usa para autenticar; eso vive en `UserCredentials`). | Sahiam1810 |
| 2026-09-03 | *(sin commit)* | **Appointments — fix de reprogramación con solapamiento**: `ConfirmAppointmentActionCodeCommand` (paso de confirmación del flujo de OTP de autoservicio del cliente) llamaba a `appointment.Reschedule(...)` sin validar solapamiento contra el nuevo horario — a diferencia de `CreateAppointmentCommandHandler`/`UpdateAppointmentCommandHandler`, que sí llaman `HasOverlappingAppointmentAsync`. Un cliente podía reprogramar su cita a un horario ya ocupado por otra cita de la misma mascota o del mismo veterinario. Corregido agregando la misma validación (excluyendo la propia cita) antes del `Reschedule`; test nuevo en `ConfirmAppointmentActionCodeCommandHandlerTests`. También se generó y aplicó la migración `AddAppointmentClientOtpSelfService` (tabla `APPOINTMENT_ACTION_VERIFICATION_SESSIONS` + columnas `CLIENTS.PHONE_NUMBER`/`APPOINTMENTS.REQUESTER_PHONE_NUMBER`) que faltaba desde el commit `ca58950`, el cual había borrado migración y tests de esta feature sin avisar en el mensaje. | Sahiam1810 |
| 2026-09-03 | *(sin commit)* | **`AppointmentStatusHistoriesController` — fix del bypass de transición de estado (VET-04/05)**: a diferencia del endpoint canónico `PATCH /api/appointments/{id}/status`, el CRUD de `AppointmentStatusHistoriesController` no aplicaba ninguna regla de transición: `POST` podía crear cualquier salto de estado (incluso `CANCELADA → AGENDADA`) sin comentario; `PUT` permitía reescribir a qué `AppointmentId`/`StatusId`/`ClientPetId` apuntaba una fila de historial ya registrada, falsificando el historial; `DELETE` podía borrar la fila vigente (la más reciente) dejando a `Appointment.StatusId` desincronizado del historial real, sin dejar rastro. Fix: extraídas las reglas de transición compartidas a `Application/Appointments/AppointmentStatusTransitionRules.cs` (usadas ahora por `UpdateAppointmentStatusCommandHandler` y por `CreateAppointmentStatusHistoryCommandHandler`); `UpdateAppointmentStatusHistoryCommandHandler` ahora rechaza (400) cualquier cambio de `AppointmentId`/`StatusId`/`ClientPetId`, solo el `Comment` es editable; `DeleteAppointmentStatusHistoryCommandHandler` rechaza (409) borrar la entrada vigente de la cita (nuevo método `IAppointmentStatusHistoryRepository.GetByAppointmentIdAsync`, ordenado por `CreatedAt` descendente). 14 tests nuevos en `tests/Application.Tests/AppointmentStatusHistories/` (antes no existía ningún test para este módulo). De paso: confirmado que la sincronización de `Appointment.StatusId` en `CreateAppointmentStatusHistoryCommandHandler` que VET-04/05 reportaba como faltante **ya estaba implementada** (el reporte original estaba desactualizado); lo que sí faltaba y se corrigió hoy era la validación de transición. | Sahiam1810 |
| 2026-09-03 | *(sin commit)* | **Restaurados los tests de la feature de OTP de autoservicio del cliente**, borrados sin aviso junto con su migración por el commit `ca58950` ("...sin migracion"): `tests/Application.Tests/Appointments/RequestAppointmentActionCodeCommandHandlerTests.cs` (8 tests — sesión creada y código despachado cuando no hay sesión activa; 401 cuando el teléfono no coincide con el de la cita; 404 cuando la cita no existe; 400 cuando falta el payload de reagendado; error de JSON cuando el payload es inválido; 409 cuando ya se envió un código y no pasó el intervalo de reenvío; cancela la sesión vieja y emite una nueva pasado ese intervalo; 409 cuando el proveedor de envío falla) y `tests/Application.Tests/Verification/AppointmentActionVerificationSessionTests.cs` (24 tests — cubre el estado del `Start`/`RegisterFailedAttempt`/`Complete`/`Cancel`/`Expire` de `AppointmentActionVerificationSession`, incluida la validación de formato SHA-256 de los hashes y el bloqueo al alcanzar el máximo de intentos). `dotnet test` en verde: **686/686** (461 Application + 69 Infrastructure + 156 Api). | Sahiam1810 |

Todo lo anterior está en `develop`, **excepto la fila de Auditoría de Users** (código + migración generada, sin commitear todavía). `dotnet test` en verde: 465/465.

---

## 4. Pendiente y no asignado — corregido contra el código real

Lista original ajustada: varios ítems que se daban por pendientes **ya están resueltos**.

### ✅ Ya no está pendiente: SEC-01
Re-verificado 2026-09-03 contra el código actual: `GetMyAppointmentsQueryHandler` ya vuelve a `Array.Empty<Appointment>()` cuando `client is null`, y además ahora filtra explícitamente por `ClientPetIds` del cliente (`GetByClientPetIdsAsync`) en vez de `GetAllAsync()`. Resuelto en algún commit posterior a PR #85/#86/#87 (no identificado con exactitud cuál). Quitar de cualquier lista de pendientes.

### ✅ Ya no está pendiente: SEC-02
`PATCH /api/usercredentials/{id}/change-password` ya valida por diseño: quedó exclusivo de `SuperAdminOnly` (ya no `RequirePermission("Usuarios", Edit)`, el Administrador normal perdió el acceso). Nuevo `PATCH /api/auth/me/password` como autoservicio para cualquier rol autenticado, resolviendo las credenciales por el `sub` del propio JWT. Implementado hoy, ver §3. Quitar de cualquier lista de pendientes.

### ✅ Ya no está pendiente: `GetEffectivePermissionQuery` sin exponer
Se agregó `GET /api/auth/permissions` hoy (`a0a4afa`). Quitar de cualquier lista de pendientes.

### ✅ Ya no está pendiente: Diagnostics con auth mixta
Migración completa en `82dbf04`. Quitar de cualquier lista de pendientes.

### ✅ Ya no está pendiente (hallazgo y fix del mismo día, Auth): registro sin perfil de `Client`
`POST /api/auth/register` solo creaba `Users`/`UserAccounts`/`UserCredentials` — el usuario auto-registrado (rol "Cliente") no tenía forma de aparecer en `/clients/me`, `/pets/mine` ni `/appointments/mine`, y no había ningún reporte previo de esto. Corregido hoy: exige `IdentificationNumber` y crea el `Client` en la misma transacción. Ver §3.

### ✅ Ya no está pendiente (hallazgo y fix del mismo día, Auth): rate limiting de Login/Register/Refresh sin partición
`Program.cs` armaba el rate limiting real con un bloque hardcodeado sin partición (un solo contador global por policy, compartido por todos los clientes) mientras la implementación particionada por usuario/IP (`AddApiRateLimiting`) existía pero nunca se invocaba. Corregido hoy. Ver §3 y la nueva sección "Rate limiting" en §1.

### ✅ Ya no está pendiente: 🔴 P0 `RolesController` roto
Módulo "Roles" creado (fila 17 en `MODULES`, vía `POST /api/modules` como SuperAdmin) y `ROLE_PERMISSIONS` actualizado: Administrador con V C E D, ningún otro rol tiene fila. Probado en vivo hoy contra la base local — ver §3 para el detalle exacto de la verificación (Administrador crea/lista/edita/elimina roles; Veterinario recibe 403; Administrador sigue sin acceso a `RolePermissionsController`/`UserPermissionsController`/escritura de `ModulesController`). Falta comitear `database/seeds/role_permissions_seed.sql` (el dato ya existe en la DB local, creado vía API).

### ✅ Ya no está pendiente: VET-04/05 — `AppointmentStatusHistoriesController` sin reglas de transición
Corregido 2026-09-03, ver §3. La sincronización de `Appointment.StatusId` en `Create` ya estaba implementada (el reporte original estaba desactualizado); lo que faltaba y se cerró hoy era la validación de transición (compartida ahora vía `AppointmentStatusTransitionRules`), la restricción de `Update` a solo `Comment`, y el bloqueo de `Delete` sobre la entrada vigente. Quitar de cualquier lista de pendientes.

### ✅ Ya no está pendiente: VET-01 a VET-08 (completo)
Re-verificado 2026-09-03: **los 8 ítems ya están resueltos**, aparentemente vía PR #80 (`feat/veterinarian-appointment-ownership`), #82 (tests), #83 (pets), #84 (`feat/appointment-medical-record`) y #86/#87 — ninguno mencionado explícitamente como "cierre de VET-0X" en su mensaje de commit, así que quedó disperso sin traza directa a los ítems originales, pero el código actual los cubre:
- VET-01: `IVeterinarianRepository.GetByUserIdAsync` existe y se usa (`GetMyVeterinarianQuery`, `GetMyAppointmentsAsVeterinarianQuery`, `CreateAppointmentMedicalRecordCommandHandler` vía `AppointmentVeterinarianOwnership`).
- VET-02/03: `GET /api/veterinarians/me` existe (`VeterinariansController.GetMe`, `RequirePermission("Veterinarios", View)`).
- VET-03/04: `GET /api/appointments/me` existe (`AppointmentsController.GetMe`), paginado, con filtros `from`/`to`, usando `AppointmentVeterinarianOwnership`/`ShouldEnforceVeterinarianOwnership` para que un Veterinario solo vea lo suyo (Admin/Recepcionista/SuperAdmin no filtran).
- VET-05/06: `CreateAppointmentMedicalRecordCommandHandler` (nuevo, vía `POST /api/appointments/{id}/medical-record`) valida ownership del veterinario (`AppointmentVeterinarianOwnership.EnsureAsync`), que no exista ya una historia clínica para esa cita, y que el diagnóstico esté activo. **Pero ver el hallazgo nuevo de hoy más abajo** — el endpoint viejo `POST /api/medicalrecords` sigue vivo en paralelo y no tiene las mismas dos últimas validaciones.
- VET-08: `PaginatedResult`/`PaginationMetadata` ya están conectados — los usa `GetMyAppointmentsAsVeterinarianQuery`/`AppointmentsController.GetMe`.

Quitar todo VET-01 a VET-08 de cualquier lista de pendientes.

### ✅ Ya no está pendiente: Historias clínicas — dos rutas de creación con validación divergente
Corregido 2026-09-03. Decisión: eliminar el endpoint viejo en vez de portarle las validaciones que le faltaban, ya que `POST /api/appointments/{appointmentId}/medical-record` cubre el mismo caso de uso completo. Se borró `POST /api/medicalrecords` (`MedicalRecordsController.Create`) y todo lo que quedaba huérfano detrás: `CreateMedicalRecordCommand`/`CreateMedicalRecordCommandHandler`/`CreateMedicalRecordCommandValidator`, los DTOs `CreateMedicalRecordRequest`/`CreateMedicalRecordResponse`, el mapeo `ToCommand`, y sus tests (`CreateMedicalRecordCommandHandlerTests`, 2 tests). `MedicalRecordsController` ahora es de solo lectura (`GetAll`/`GetById`), consistente con el propio Swagger que ya decía "historia clínica inmutable". `MedicalRecordResponse`/`ToResponse` (usados por `GetAll`/`GetById`) no se tocaron. `dotnet build` limpio, `dotnet test`: 684/684 (459 Application + 69 Infrastructure + 156 Api; baja de 686 por los 2 tests borrados junto con el handler que probaban).

### Pendientes reales que siguen abiertos (previos a esta ronda, re-verificados 2026-09-03)
- **PII en `GET /api/clients/by-identification/{identificationNumber}`**: sigue anónimo (`[AllowAnonymous]`, solo protegido por rate limiting) y sigue devolviendo `ClientResponseDto` completo (`Address`, `PhoneNumber`, `UserId`, `RegistrationDate`) a cualquiera que conozca/adivine un número de identificación válido. Es una decisión de diseño ya discutida (necesaria para que el chatbot identifique clientes sin JWT), pero el nivel de exposición de PII no se ha reconsiderado. P1 medio.
- **`Api/Common/Errors/ApiErrorResultFilter.cs` sigue sin registrarse** como filtro global de MVC (confirmado de nuevo hoy, `grep` no encuentra ninguna referencia de registro). Los DTOs que usan `DataAnnotations` en vez de FluentValidation devuelven el `ValidationProblemDetails` de ASP.NET en vez del `ApiErrorResponse` canónico ante un 400 de validación automática. Además de los módulos ya reportados (Clients, Pets, ClientsPets, Specialties, Modules, RolePermissions, UserPermissions), la ronda de hoy sumó: `ConversationStatuses`, `MessageTypes`, `SenderTypes`, `EscalationStatuses`, `Priorities`. P2 bajo (sistémico, requiere una decisión de arquitectura: registrar el filtro, o migrar todos esos DTOs a FluentValidation).
- **Sin validación de existencia de FK antes de guardar** en `CreateVeterinarianCommandHandler`/`UpdateVeterinarianCommandHandler` (`UserId`, `SpecialtyId`) y `CreateServiceCommandHandler` (`TypeServiceId`) — re-verificado hoy: si el Guid no existe, la FK real en Oracle (`HasForeignKey`, confirmada en `VeterinarianConfiguration`/`ServiceConfiguration`) rechaza el insert y `GlobalExceptionHandler` lo traduce a 409 genérico, así que **no hay riesgo de integridad** (downgradeado de la severidad original) — es una molestia de UX/API (un 409 genérico en vez de un 404 puntual con mensaje claro). P2 bajo.
- **Sin índice único en BD para `Veterinarians.UserId` ni `Clients.UserId`** (re-verificado hoy: solo `LicenseNumber`/`IdentificationNumber` son únicos) y sin chequeo a nivel de aplicación tampoco (`CreateClientCommandHandler`/`CreateVeterinarianCommandHandler` no verifican si el `UserId` ya tiene un perfil). Un mismo `User` podría terminar con dos filas de `Client` (o de `Veterinarian`), y como `GetByUserIdAsync` usa `FirstOrDefaultAsync` sin orden explícito, el comportamiento de `/clients/me`, `/pets/mine`, `/appointments/mine`, `/veterinarians/me` sería no determinístico en ese caso. P1 medio.
- Todo esto sigue pendiente de aprobación formal antes de implementarse — no empiecen a corregirlo sin visto bueno explícito.

---

## 6. Auditoría del subsistema chatbot (Chat / Escalamientos / IA-Agente / Telegram) — 2026-09-03

Estos ~28 controllers **nunca habían sido auditados** contra este documento (§2 cubre explícitamente solo los "26 controllers no-chatbot"). Revisión completa por 4 sub-auditorías en paralelo, Domain+Application+Infrastructure+Api. **Cero hallazgos P0 en las 4.** Resumen — el detalle completo de cada hallazgo vive en la conversación que generó esta ronda, no transcrito aquí para no duplicar contenido; pedir el reporte completo si hace falta el detalle línea por línea.

**Patrón transversal más repetido (no es un hueco de seguridad):** casi todo el subsistema chatbot (Chat, Escalamientos, IA y Agente, Catálogos del Chat — los 4 módulos que sí existen como filas en `MODULES`) sigue protegido con `[Authorize(Policy = AuthorizationPolicies.AdminOnly)]` en vez de `[RequirePermission]`. Es **deliberado y está documentado** en el propio `database/seeds/role_permissions_seed.sql` ("no incluye los módulos del chatbot — quedan pendientes hasta que se retome esa parte del proyecto"). Efecto colateral funcional, no de seguridad: Veterinario/Recepcionista/Auxiliar no pueden operar como agentes humanos de chat vía esta API todavía, solo Administrador/SuperAdmin.

### Chat en vivo (`ChatConversations`, `ChatMessages`, `ChatParticipants`, `ChatAttachments`, `ChatUserProfiles`, `ConversationStatuses`, `MessageTypes`, `SenderTypes`)
Mejor implementado: `ChatUserProfileController` (value objects con validación completa) y `CreateChatMessageCommandHandler` (valida pertenencia real del participante a la conversación). Hallazgos P1 medio: `ChatConversation.Close` acepta un `ClosedBy` arbitrario del body sin derivarlo del JWT ni validar que exista; `ChatParticipant.AiModelId` no tiene FK en EF ni se valida su existencia (a diferencia de `ChatUserProfileId`/`AgentHumanId`, que sí). P2 bajo: 5 controllers con el patrón viejo `is null ? NotFound()` sin migrar (straggler del refactor `6cd7068`); `ChatMessage.Content` sin límite de longitud; `ChatAttachment.FileUrl`/`FileType` sin validar formato.

### Escalamientos (`ChatEscalations`, `ChatEscalationAssignments`, `ChatEscalationResolutions`, `ChatEscalationStatusHistories`, `EscalationStatuses`, `Priorities`)
**Hallazgo más importante de toda la ronda** — es la misma clase de bug que motivó el fix de hoy en `AppointmentStatusHistoriesController` (VET-04/05), pero acá nunca se corrigió: `ChatEscalationStatusHistory` no tiene ninguna regla de transición (ni el equivalente a `AppointmentStatusTransitionRules`), `Update`/`Delete` de una entrada de historial no tienen ninguna restricción (se puede reescribir o borrar la entrada vigente libremente), y **no hay ninguna sincronización** entre el historial y `ChatEscalation.EscalationStatusId` — son dos fuentes de verdad completamente desacopladas. Además, `ChatEscalationResolution.ResolvedBy` no tiene FK ni validación de identidad real (cualquier Guid es aceptado), no hay unicidad por escalamiento (se pueden crear N resoluciones para el mismo), y crear una resolución no marca el escalamiento como resuelto. Todo P1 medio (gateado por rol Administrador, es integridad de datos/auditoría, no un hueco de autorización). Recomendado: aplicar el mismo patrón de fix que `AppointmentStatusHistoriesController` (reglas de transición compartidas, `Update` restringido a campos no identitarios, `Delete` bloqueado sobre la entrada vigente, sync explícito con el estado del padre).

### IA / Agente conversacional (`Agent`, `AgentHumans`, `AiModels`, `AiRunStatuses`, `ChatAiRuns`, `ChatAiRunErrors`, `ChatAiRunMetrics`, `ChatConversationAiSettings`, `ChatConversationAssignments`, `ProviderModelsAi`)
Se buscó explícitamente exposición de API keys/credenciales de proveedores LLM — **no se encontró ninguna**; el backend .NET no llama directo al proveedor, reenvía a un microservicio interno separado sin secretos en esta base. `AgentMessagesController` (el endpoint que dispara llamadas a LLM) no tiene ownership/IDOR (`PersistentConversationContextProvider` valida pertenencia real a la conversación) pero **no tiene rate limiting dedicado** — solo el límite global genérico (300/60s), insuficiente como control de costo/abuso para un endpoint que factura por token. P1 medio también: `CreateAgentHumanCommandHandler` no valida que el `UserId` tenga un rol de staff antes de habilitarlo como agente elegible para asignación. `ChatConversationAssignmentController` está bien protegido contra doble asignación incluso bajo concurrencia (PK 1:1 real en BD). `AiRunStatusesController` es el único controller de todo el backend-chatbot ya alineado 100% al patrón canónico de excepciones.

### Telegram (webhook, vinculación por código, registro conversacional, formulario de completar registro)
**El subsistema mejor construido en materia de seguridad de toda la ronda.** Secreto de webhook validado en tiempo constante (`CryptographicOperations.FixedTimeEquals`) y correctamente wireado; códigos de vinculación y tokens de registro con 192-256 bits de entropía, hasheados en reposo, de un solo uso y expirables; sin enumeración de cuentas en las respuestas del bot; idempotencia robusta por `update_id` de Telegram con claim atómico anti-doble-procesamiento; sin SSRF/inyección en el cliente HTTP saliente; sin secretos comiteados en `appsettings`. Único P1 medio: `TELEGRAM_USER_LINKS` no tiene índice único en BD para `PersonId`/`TelegramUserId` — la invariante "un solo vínculo activo" depende solo de un chequeo a nivel de aplicación (check-then-act), frágil ante concurrencia futura si el worker se escala a más de una instancia. P2 bajo: `TelegramLinkCodesController.Create` sin rate limit dedicado (bajo impacto, el código ya es imposible de adivinar); posible side-channel de timing en la vinculación por OTP de email; el flujo `/start <code>` no redacta el texto del mensaje tan rápido como los flujos de OTP.

### Resumen total de la ronda (solo hallazgos nuevos de esta auditoría — no recuenta lo ya documentado en §4)

| Severidad | Chat en vivo | Escalamientos | IA/Agente | Telegram | Total |
|---|---|---|---|---|---|
| P0 | 0 | 0 | 0 | 0 | **0** |
| P1 medio | 9 | 7 | 2 | 1 | **19** |
| P2 bajo | 9 | 3 | 5 | 3 | **20** |

---

## 7. Convención de reporte de hallazgos

Si encuentras algo nuevo durante tu revisión:

1. **No lo corrijas tú** si el módulo no es el que te asignaron — repórtalo para que Ceteno/Kevin lo centralicen.
2. Documéntalo con este formato mínimo:
   - **Módulo**
   - **Endpoint específico** (método + ruta)
   - **Comportamiento esperado vs. real**
   - **Severidad** (P0 crítico / P0 alto / P1 medio / P2 bajo)
3. Antes de reportar algo como "pendiente", revisa `git log -- <archivo>` — varios de los hallazgos de hace 2 días ya se resolvieron (ver §3), y lo contrario también pasó (SEC-01 se resolvió y se revirtió el mismo día). No asumas que el estado de un archivo es el que viste la última vez que lo revisaste.
