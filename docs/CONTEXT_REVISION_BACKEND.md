# Contexto de revisión — Backend Huellitas

**Propósito:** que Gallo, Tomás y Sahiam puedan revisar el backend en paralelo, cada uno en sus módulos asignados, sin preguntar contexto adicional y sin duplicar trabajo ya hecho.

**Estado del repo a la fecha de este documento:** `develop` @ `2522161` (2026-09-01), más una auditoría completa del módulo Auth hecha el mismo día **todavía sin commitear** (ver detalle en §3, fila "Auditoría Auth"). Build y `dotnet test` en verde: **424/424** (257 Application + 61 Infrastructure + 106 Api) — incluye los cambios de Auth sin commitear. El resto del árbol de trabajo (fuera de los archivos tocados por esa auditoría) sigue limpio.

**Cómo se armó este documento:** no es un resumen de memoria — cada afirmación de las secciones 2, 3 y 4 se verificó releyendo el archivo correspondiente o el commit correspondiente el mismo día que se escribió esto. Si algo cambia después de este commit, ese cambio **no** está reflejado aquí — corre `git log` sobre los archivos que te toquen antes de asumir que esto sigue vigente. Auth es la excepción: quedó auditado y cerrado hoy (§2, §3, §4), no lo vuelvan a revisar salvo que toquen esos archivos.

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
- El nombre del módulo en el atributo debe **coincidir exactamente** (case-sensitive, con tildes) con una fila en la tabla `MODULES`. Si no existe esa fila, el endpoint queda inaccesible para todo el mundo excepto SuperAdmin — ver el hallazgo crítico en la sección 4.
- El propio usuario autenticado puede ver sus permisos efectivos vía `GET /api/auth/permissions` (agregado hoy, ver sección 3).
- **Catálogo actual de módulos** (16 filas en `MODULES`, verificado por SQL): Clientes, Mascotas, Especies y Razas, Especialidades, Veterinarios, Citas, Historiales Clínicos, Servicios, Estados de Cita, Cuentas y Pagos, Notificaciones, Usuarios, Chat, Escalamientos, IA y Agente, Catálogos del Chat. **"Roles" y "Roles y Permisos" NO existen como módulos** — ver sección 4.
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
| `AppointmentStatusHistoriesController` | POST→`Citas:Edit`, GET→`StaffOnly` (deliberado), GET/{id}→`StaffOnly` (deliberado), PUT→`Citas:Edit`, DELETE→`Citas:Delete` | POST usa `Edit` no `Create` — "cambiar estado" se modeló como editar la cita, para que Veterinario (V+E, sin C) pueda usarlo |
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
| **`RolesController`** | Los 5 endpoints→`RequirePermission("Roles", ...)` | 🔴 **ROTO — no existe módulo "Roles". Ver hallazgo crítico en §4.** |
| `ServicesController` | Los 5 endpoints→`Servicios:<acción>` | Comparte módulo "Servicios" con `TypeServicesController` |
| `SpecialtiesController` | Los 5 endpoints→`Especialidades:<acción>` | |
| `SpeciesController` | Los 5 endpoints→`Especies y Razas:<acción>` | |
| `StatusAppointmentsController` | Los 5 endpoints→`Estados de Cita:<acción>` | |
| `TypeServicesController` | Los 5 endpoints→`Servicios:<acción>` | |
| `UserAccountsController` | Los 5 endpoints→`Usuarios:<acción>` | |
| `UserCredentialsController` | POST→`Usuarios:Create`, GET/{id}/by-account→`...View`, PATCH change-password→`SuperAdminOnly` | SEC-02 implementado 2026-09-01: reset de contraseña ajena exclusivo de SuperAdmin (ya no `Usuarios:Edit`); autoservicio movido a `PATCH /api/auth/me/password` |
| `UserPermissionsController` | Los 6 endpoints→`SuperAdminOnly` | |
| `UserTokensController` | POST/GET/{id}/by-account/DELETE→`Usuarios:<acción>` | |
| `UsersController` | POST→`Create`, GET/GET{id}→`View`, PUT/deactivate/activate→`Edit` | |
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
| 2026-09-01 | `82dbf04` | `RequirePermission` en Diagnostics (completo) y **Roles** — este último quedó roto porque no se creó el módulo correspondiente, ver §4 | (verificar autor, no capturado en la muestra revisada) |
| 2026-09-01 | `a0a4afa` | Nuevo `GET /api/auth/permissions`, expone `GetEffectivePermissionQuery` (como `GetUserEffectivePermissionsQuery`, todos los módulos a la vez) al usuario autenticado | spostre |
| 2026-09-01 | *(sin commit)* | **Auditoría completa de Auth** (Domain/Application/Infrastructure/Api), todo verificado línea por línea y con tests nuevos: (1) **SEC-02** implementado — `PATCH /api/usercredentials/{id}/change-password` ahora `SuperAdminOnly`, nuevo `PATCH /api/auth/me/password` de autoservicio para cualquier rol; (2) `POST /api/auth/register` exige `IdentificationNumber` (misma regla que `CreateClientCommandValidator`) y crea el `Client` dentro de la misma transacción que `Users`/`UserAccounts`/`UserCredentials` — antes el usuario auto-registrado quedaba sin perfil de cliente y `/clients/me`, `/pets/mine`, `/appointments/mine` le daban 404 para siempre; (3) rate limiting de Login/Register/Refresh/TelegramWebhook reconectado: `Program.cs` usaba un bloque hardcodeado y **sin partición** (contador global compartido por todos los clientes), ahora usa `AddApiRateLimiting`/`UseApiRateLimiting`, particionado por usuario/IP y configurable vía `appsettings.json` (sección `RateLimiting`, antes inexistente); (4) `GET /api/auth/permissions` ya no da 401 a un SuperAdmin autenticado, devuelve los 4 flags en `true` para todos los módulos; (5) limpieza: `AuthenticationErrors.ForbiddenTokenOwner` y la rama `Forbid()` en `AuthController.Revoke` eran código muerto (`RevokeAsync` nunca distingue ese caso) — eliminados; `UserTokens.IsExpired` pasó de `DateTime.UtcNow` directo a `IsExpiredAsOf(TimeProvider)`, con `AuthenticationService.RefreshAsync` usando el `TimeProvider` ya inyectado. | Sahiam1810 |

Todo lo anterior está en `develop` **excepto la fila de Auditoría de Auth, que sigue sin commitear** (ver §1 encabezado). `dotnet test` en verde: 424/424.

---

## 4. Pendiente y no asignado — corregido contra el código real

Lista original ajustada: dos ítems que se daban por pendientes **ya están resueltos**, y apareció un hallazgo crítico que no estaba en ningún reporte anterior.

### 🔴 P0 — Nuevo, no reportado antes: `RolesController` roto
`RolesController` usa `RequirePermission("Roles", ...)` en los 5 endpoints, pero **no existe ningún módulo "Roles" en la tabla `MODULES`** (verificado por SQL, 16 filas, ninguna se llama "Roles"). Resultado: `GetEffectivePermissionQueryHandler` nunca encuentra el módulo, el permiso efectivo siempre da `false`, y **nadie except SuperAdmin puede crear, ver, editar o eliminar roles ahora mismo** — ni el Administrador. Esto bloquea cualquier flujo de onboarding de roles nuevos. Necesita: o bien crear el módulo "Roles" y asignarle permisos en la matriz, o revertir `RolesController` a la policy que tenía antes (`AdminOnly`), según decida la líder.

### SEC-01 — confirmado, activo hoy
Ver commit `9fc66ff` en §3. `GET /api/appointments/mine` devuelve todas las citas a cualquier Admin/Vet/Recepcionista que la llame (están en `ClinicalHistoryReadOnly`, que los incluye). El fix es una línea: volver a `Array.Empty<Appointment>()` en vez de `GetAllAsync()` cuando `client is null` — pero antes de revertirlo, confirmar con quien lo cambió (Ksanti-monsalve) si había una razón de negocio detrás, porque el mensaje del commit sugiere que fue intencional ("Enhanced logic to allow Admin or Staff users...").

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

### Pendientes reales que siguen abiertos (de los informes de VET-01 a VET-08, ver conversación previa con la líder sobre el "Plan de correcciones backend — Perfil y operación veterinaria")
- VET-01: `IVeterinarianRepository` sigue sin `GetByUserIdAsync`.
- VET-02/03: no existe `GET /api/veterinarians/me`.
- VET-03/04: no existe `GET /api/appointments/me` (agenda propia del veterinario, con filtros/paginación).
- VET-04/05: `CreateAppointmentStatusHistoryCommandHandler` sigue sin actualizar `Appointment.StatusId` — solo inserta la fila de historial, la cita nunca refleja el cambio.
- VET-05/06: `CreateMedicalRecordCommandHandler` sigue sin validar que la cita pertenezca al veterinario autenticado, ni ninguna otra validación.
- VET-08: `PaginatedResult`/`PaginationMetadata` existen en `Application/Common/Models/` pero cero controllers los usan — infraestructura sin conectar.
- Todo esto sigue pendiente de aprobación formal de la líder según el documento que ella está revisando — no empiecen a implementarlo sin ese visto bueno explícito.

---

## 5. Convención de reporte de hallazgos

Si encuentras algo nuevo durante tu revisión:

1. **No lo corrijas tú** si el módulo no es el que te asignaron — repórtalo para que Ceteno/Kevin lo centralicen.
2. Documéntalo con este formato mínimo:
   - **Módulo**
   - **Endpoint específico** (método + ruta)
   - **Comportamiento esperado vs. real**
   - **Severidad** (P0 crítico / P0 alto / P1 medio / P2 bajo)
3. Antes de reportar algo como "pendiente", revisa `git log -- <archivo>` — varios de los hallazgos de hace 2 días ya se resolvieron (ver §3), y lo contrario también pasó (SEC-01 se resolvió y se revirtió el mismo día). No asumas que el estado de un archivo es el que viste la última vez que lo revisaste.
