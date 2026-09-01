# Guía de resolución — Módulos operación Cliente / Veterinario

**Para:** Santiago Centeno (backend)  
**Origen:** Auditoría Tomás sobre `CONTEXT_REVISION_BACKEND.md`  
**Repo:** `veterinarian-backend` rama `develop`  
**Fecha:** 2026-09-01  

Esta guía **no es un informe de hallazgos**: es el plan de trabajo para **cerrar cada problema** en el código y, cuando aplique, validar en Oracle las tablas involucradas.

---

## 0. Antes de tocar código

### 0.1 Entorno

```powershell
cd C:\Users\ESSA8\Documents\veterinarian-backend
git checkout develop
git pull origin develop
dotnet build src/Api/Api.csproj
dotnet test
```

Debe quedar en verde (401 tests al momento de la auditoría).

### 0.2 Rama de trabajo sugerida

```powershell
git checkout -b fix/operacion-cliente-veterinario
```

Un PR por fase (o uno solo si la líder prefiere) — ver orden de prioridad al final.

### 0.3 Mapa rápido: problema → tablas Oracle → capa código

| ID | Problema | Tablas Oracle principales | Archivos clave |
|---|---|---|---|
| **SEC-01** | `/appointments/mine` filtra mal para staff | `APPOINTMENTS`, `CLIENTS`, `CLIENTS_PETS`, `USER_ACCOUNTS` | `GetMyAppointmentsQuery.cs`, `AppointmentsController.cs` |
| **VET-01** | No hay lookup vet por usuario | `VETERINARIANS` (`USER_ID`) | `IVeterinarianRepository.cs`, `VeterinarianRepository.cs` |
| **VET-02/03** | Falta `GET /veterinarians/me` | `VETERINARIANS`, `USERS`, `SPECIALTIES` | `VeterinariansController.cs` + nuevo Query |
| **VET-03/04** | Falta `GET /appointments/me` | `APPOINTMENTS` (`VETERINARIAN_ID`, `STATUS_ID`) | `IAppointmentRepository.cs`, `AppointmentsController.cs` |
| **VET-04/05** | Historial no actualiza cita | `APPOINTMENT_STATUS_HISTORIES`, `APPOINTMENTS` | `CreateAppointmentStatusHistoryCommandHandler.cs` |
| **VET-05/06** | Historia clínica sin validar cita/vet | `MEDICAL_RECORDS`, `APPOINTMENTS`, `VETERINARIANS` | `CreateMedicalRecordCommandHandler.cs` |
| **TOM-01** | Cuenta JWT inexistente → lista completa | (misma lógica que arriba) | `GetAllMedicalRecordsQueryHandler.cs`, `GetAllVaccinationsQueryHandler.cs` |
| **TOM-02** | PUT vacunas sin scoping dueño | `VACCINATIONS`, `CLIENTS_PETS` | `UpdateVaccinationCommandHandler.cs` |
| **TOM-03** | POST historial sin validar FKs | `APPOINTMENT_STATUS_HISTORIES`, `APPOINTMENTS`, `STATUS_APPOINTMENTS`, `CLIENTS_PETS` | `CreateAppointmentStatusHistoryCommandValidator.cs` + Handler |
| **VET-08** | Paginación sin usar | N/A (solo API) | `PaginatedResult.cs`, repos + controllers |

### 0.4 Comprobar el diccionario Oracle (siempre antes de asumir nombres)

Conéctate como `TOMDEVV` a `FREEPDB1` y verifica que las tablas existen en **tu** esquema:

```sql
-- Tablas de este alcance (no copies "nombretabla" de tutoriales)
SELECT table_name
FROM user_tables
WHERE table_name IN (
  'APPOINTMENTS',
  'APPOINTMENT_STATUS_HISTORIES',
  'CLIENTS',
  'CLIENTS_PETS',
  'MEDICAL_RECORDS',
  'VACCINATIONS',
  'VETERINARIANS',
  'USER_ACCOUNTS',
  'STATUS_APPOINTMENTS',
  'MODULES',
  'ROLE_PERMISSIONS'
)
ORDER BY table_name;
```

Columnas críticas para las correcciones:

```sql
-- Citas: estado actual vive aquí (VET-04/05)
SELECT column_name, data_type
FROM user_tab_columns
WHERE table_name = 'APPOINTMENTS'
  AND column_name IN ('APPOINTMENT_ID', 'VETERINARIAN_ID', 'CLIENT_PET_ID', 'STATUS_ID');

-- Veterinario ligado al usuario de login (VET-01)
SELECT column_name FROM user_tab_columns
WHERE table_name = 'VETERINARIANS' AND column_name = 'USER_ID';

-- Historial de estados (TOM-03)
SELECT column_name FROM user_tab_columns
WHERE table_name = 'APPOINTMENT_STATUS_HISTORIES';
```

### 0.5 Usuarios demo para probar (local)

| Rol | Email | Password |
|---|---|---|
| Cliente | `cliente@huellitas.com` | `Huellitas2026!` |
| Veterinario | `veterinario@huellitas.com` | `Huellitas2026!` |
| Administrador | `admin@huellitas.com` | `Huellitas2026!` |

API base: `http://localhost:5233`

---

## FASE 1 — P0: SEC-01 + TOM-04 (`/api/appointments/mine`)

### Problema

`GET /api/appointments/mine` usa policy `ClinicalHistoryReadOnly` (incluye Admin/Vet/Recep).  
Si el usuario **no** tiene fila en `CLIENTS`, el handler devuelve **todas** las citas (`GetAllAsync()`). Eso es fuga de datos.

**Referencia actual (mal):** `src/Application/Appointments/UseCases/GetMyAppointmentsQuery.cs` líneas 28-32.

### Objetivo

Comportarse igual que `/api/clients/me` y `/api/pets/mine`: **solo dueños con perfil Cliente**.

### Pasos

**1.** Abrir `src/Application/Appointments/UseCases/GetMyAppointmentsQuery.cs`.

**2.** Reemplazar el bloque `if (client is null) { return GetAllAsync... }` por:

```csharp
if (client is null)
{
    throw new NotFoundException(
        "El usuario autenticado no tiene un perfil de cliente asociado.");
}
```

(Mismo mensaje que `GetMyPetsQueryHandler`.)

**3.** Abrir `src/Api/Appointments/Controllers/AppointmentsController.cs`.

**4.** En `GetMine`, cambiar la policy:

```csharp
// Antes:
[Authorize(Policy = AuthorizationPolicies.ClinicalHistoryReadOnly)]

// Después:
[Authorize(Policy = AuthorizationPolicies.ClientOnly)]
```

**5.** Actualizar `EndpointDescription` del Swagger: dejar claro que es **portal del dueño**, no agenda staff.

**6.** Coordinar con quien hizo el commit `9fc66ff` (Ksanti-monsalve): si staff necesitaba ver citas, debe usar `GET /api/appointments` (`StaffOnly`), no `/mine`.

### Verificación

```powershell
# Login cliente → debe 200 con solo sus citas (o [])
$tokenCliente = (Invoke-RestMethod -Uri "http://localhost:5233/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"cliente@huellitas.com","password":"Huellitas2026!"}').accessToken
Invoke-RestMethod -Uri "http://localhost:5233/api/appointments/mine" -Headers @{ Authorization = "Bearer $tokenCliente" }

# Login veterinario → debe 404 (ya no ve todas las citas por /mine)
$tokenVet = (Invoke-RestMethod -Uri "http://localhost:5233/api/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"veterinario@huellitas.com","password":"Huellitas2026!"}').accessToken
try {
  Invoke-RestMethod -Uri "http://localhost:5233/api/appointments/mine" -Headers @{ Authorization = "Bearer $tokenVet" }
} catch { $_.Exception.Response.StatusCode }  # Esperado: NotFound
```

**Test unitario sugerido:** `tests/Application.Tests/Appointments/GetMyAppointmentsQueryHandlerTests.cs`

- Caso A: cuenta + cliente + mascotas → solo citas de esas `CLIENT_PET_ID`.
- Caso B: cuenta sin `CLIENTS` → `NotFoundException`.
- Caso C: cuenta inexistente → `NotFoundException`.

### Commit sugerido

`fix(appointments): restringir /mine a ClientOnly y perfil de cliente`

---

## FASE 2 — P1: VET-01 + VET-02/03 (`GET /api/veterinarians/me`)

### Problema

- `VETERINARIANS.USER_ID` existe en dominio/BD, pero el repositorio no expone `GetByUserIdAsync`.
- El FE veterinario hoy hace `GET /api/veterinarians` y filtra en cliente (frágil).

### Pasos

**1.** En `src/Application/Veterinarians/Abstraction/IVeterinarianRepository.cs`, agregar:

```csharp
Task<Veterinarian?> GetByUserIdAsync(
    Guid userId,
    CancellationToken cancellationToken);
```

**2.** En `src/Infrastructure/Veterinarians/Repositories/VeterinarianRepository.cs`, implementar:

```csharp
public Task<Veterinarian?> GetByUserIdAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
    => _context.Set<Veterinarian>()
        .Include(x => x.User)
        .Include(x => x.Specialty)
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
```

**3.** Crear `src/Application/Veterinarians/UseCases/GetMyVeterinarianQuery.cs` (copiar patrón de `GetMyClientQuery.cs`):

| Paso en handler | Tabla / repo |
|---|---|
| `UserAccountsRepository.GetByIdAsync(userAccountId)` | `USER_ACCOUNTS` |
| Si null → `NotFoundException` | — |
| `VeterinariansRepository.GetByUserIdAsync(account.UserId)` | `VETERINARIANS` |
| Si null → `NotFoundException("...perfil de veterinario...")` | — |
| Devolver entidad `Veterinarian` | — |

**4.** En `src/Api/Veterinarians/Controllers/VeterinariansController.cs`, agregar **antes** de `GetAll`:

```csharp
[HttpGet("me")]
[RequirePermission("Veterinarios", PermissionAction.View)]
// Resolver userAccountId desde Claims (igual que ClientsController.GetMe)
// sender.Send(new GetMyVeterinarianQuery(userAccountId))
// return Ok(veterinarian.ToResponse());
```

**5.** Índice único opcional en Oracle (solo si hay duplicados en datos demo):

```sql
SELECT user_id, COUNT(*) FROM veterinarians GROUP BY user_id HAVING COUNT(*) > 1;
-- Si hay duplicados, limpiar datos antes de crear índice único
```

### Verificación

```sql
-- Debe existir fila para el usuario demo veterinario
SELECT v.veterinarian_id, v.user_id, v.license_number, u.email
FROM veterinarians v
JOIN users u ON u.user_id = v.user_id
WHERE u.email = 'veterinario@huellitas.com';
```

Si no hay fila, el endpoint `/me` responderá 404 hasta que Recepción/Admin registre al vet en `VETERINARIANS` (dato operativo, no bug de código).

```powershell
$tokenVet = (Invoke-RestMethod ... veterinario ...).accessToken
Invoke-RestMethod -Uri "http://localhost:5233/api/veterinarians/me" -Headers @{ Authorization = "Bearer $tokenVet" }
```

### Commit sugerido

`feat(veterinarians): agregar GetByUserIdAsync y GET /api/veterinarians/me`

---

## FASE 3 — P1: VET-03/04 (`GET /api/appointments/me`)

### Problema

No existe agenda del veterinario autenticado. El staff usa `GET /api/appointments` (todo el sistema). El FE vet filtra en memoria por `veterinarianId`.

### Diseño acordado (seguir patrón `/me` existente)

Nuevo endpoint: `GET /api/appointments/me`  
Query params opcionales: `from`, `to` (UTC o local — documentar en Swagger), `page`, `pageSize` (si implementas VET-08 en la misma fase).

### Pasos

**1.** En `IAppointmentRepository` / `AppointmentRepository`, agregar:

```csharp
Task<IReadOnlyCollection<Appointment>> GetByVeterinarianIdAsync(
    Guid veterinarianId,
    DateTime? fromUtc,
    DateTime? toUtc,
    CancellationToken cancellationToken);
```

Implementación EF sobre `APPOINTMENTS` filtrando `VETERINARIAN_ID`, orden `ScheduledStart DESC`, includes iguales a `GetAllAsync`.

**2.** Crear `GetMyAppointmentsAsVeterinarianQuery` + Handler:

```
JWT sub → USER_ACCOUNTS → account.UserId
→ VETERINARIANS.GetByUserIdAsync
→ si null: NotFoundException
→ AppointmentsRepository.GetByVeterinarianIdAsync(vet.Id, from, to)
```

**3.** En `AppointmentsController`, agregar:

```csharp
[HttpGet("me")]
[RequirePermission("Citas", PermissionAction.View)]
public async Task<ActionResult<...>> GetMe(
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to,
    CancellationToken cancellationToken)
```

**Importante:** La ruta `me` debe declararse **antes** de `{id:guid}` para que ASP.NET no confunda `me` con un GUID.

**4.** Staff que no sea veterinario (Admin sin fila en `VETERINARIANS`) → `404` con mensaje claro.

### Verificación SQL

```sql
SELECT a.appointment_id, a.veterinarian_id, a.scheduled_start, sa.name AS status
FROM appointments a
JOIN status_appointments sa ON sa.status_appointment_id = a.status_id
JOIN veterinarians v ON v.veterinarian_id = a.veterinarian_id
JOIN users u ON u.user_id = v.user_id
WHERE u.email = 'veterinario@huellitas.com'
ORDER BY a.scheduled_start DESC;
```

Comparar conteo con la respuesta de `GET /api/appointments/me`.

### Commit sugerido

`feat(appointments): agenda del veterinario autenticado en GET /me`

---

## FASE 4 — P1: VET-04/05 + TOM-03 (historial de estado de cita)

### Problema A (VET-04/05)

`POST /api/appointmentstatushistories` inserta en `APPOINTMENT_STATUS_HISTORIES` pero **no actualiza** `APPOINTMENTS.STATUS_ID`. La UI y reportes leen el estado de la cita, no el último historial.

### Problema B (TOM-03)

El validator solo valida GUIDs no vacíos; no comprueba que `appointmentId`, `statusId` y `clientPetId` existan y sean coherentes.

### Pasos — Handler (transacción única)

Abrir `CreateAppointmentStatusHistoryCommandHandler.cs` y, **antes** de `SaveChangesAsync`:

**1.** Cargar la cita:

```csharp
var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
    request.AppointmentId, cancellationToken)
    ?? throw new NotFoundException("Cita médica no encontrada.");
```

**2.** Validar coherencia `clientPetId`:

```csharp
if (appointment.ClientPetId != request.ClientPetId)
{
    throw new BadRequestException(
        "La mascota indicada no corresponde a la cita.");
}
```

**3.** Cargar catálogo de estado:

```csharp
var status = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
    request.StatusId, cancellationToken)
    ?? throw new NotFoundException("Estado de cita no encontrado.");
```

**4.** Insertar historial (código actual).

**5.** Actualizar la cita — usar el método de dominio existente `Appointment.Update(...)` pasando el **nuevo** `request.StatusId` y el resto de campos actuales de `appointment` (leer propiedades antes de Update).

**6.** `AppointmentsRepository.UpdateAsync(appointment)` en la **misma** unidad de trabajo.

**7.** Un solo `SaveChangesAsync` al final.

### Pasos — Validator (opcional si la lógica ya está en handler)

Puedes dejar reglas de formato en `CreateAppointmentStatusHistoryCommandValidator.cs` y mover validaciones de existencia al handler (patrón usado en otros módulos post-refactor `6cd7068`).

### Verificación Oracle

```sql
-- Antes del POST: anotar STATUS_ID
SELECT appointment_id, status_id FROM appointments WHERE appointment_id = '<id>';

-- Después del POST: STATUS_ID debe coincidir con el enviado
SELECT appointment_id, status_id FROM appointments WHERE appointment_id = '<id>';

SELECT * FROM appointment_status_histories
WHERE appointment_id = '<id>'
ORDER BY created_at DESC;
```

### Commit sugerido

`fix(appointment-status): sincronizar STATUS_ID de APPOINTMENTS al crear historial`

---

## FASE 5 — P1: VET-05/06 (crear historia clínica)

### Problema

`POST /api/medicalrecords` no valida que:
- La cita exista.
- `ClientPetId` coincida con la cita.
- (Opcional según negocio) La cita pertenezca al veterinario autenticado.

### Pasos

**1.** Extender `CreateMedicalRecordCommand` con `Guid UserAccountId` (inyectado desde el controller, igual que en GET de vacunas).

**2.** En `CreateMedicalRecordCommandHandler`, antes de crear `MedicalRecord`:

```csharp
var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
    request.AppointmentId, cancellationToken)
    ?? throw new NotFoundException("Cita médica no encontrada.");

if (appointment.ClientPetId != request.ClientPetId)
{
    throw new BadRequestException("La mascota no corresponde a la cita.");
}

// Validación veterinario (VET-06):
var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
    request.UserAccountId, cancellationToken)
    ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

var veterinarian = await unitOfWork.VeterinariansRepository.GetByUserIdAsync(
    account.UserId, cancellationToken);

if (veterinarian is not null && appointment.VeterinarianId != veterinarian.Id)
{
    throw new UnauthorizedException(
        "La cita no está asignada al veterinario autenticado.");
}
// Si veterinarian is null → es staff (Admin): permitir según matriz de permisos
```

**3.** Validar que `DiagnosticId` exista (`DiagnosticsRepository.GetByIdAsync`).

**4.** Pasar `UserAccountId` desde `MedicalRecordsController.Create` leyendo el JWT.

### Verificación

- Vet A intenta crear historia en cita de Vet B → **401**.
- Vet crea historia en su cita → **201** + fila en `MEDICAL_RECORDS`.

```sql
SELECT mr.medical_record_id, mr.appointment_id, mr.client_pet_id
FROM medical_records mr
WHERE mr.appointment_id = '<id>';
```

### Commit sugerido

`fix(medical-records): validar cita, mascota y veterinario al crear historia`

---

## FASE 6 — P1: TOM-01 (cuenta JWT inexistente en historial clínico)

### Problema

En `GetAllMedicalRecordsQueryHandler` y `GetAllVaccinationsQueryHandler`, si `account is null` se asume staff y se devuelve **todo**.  
En `/mine` de citas/mascotas se lanza `NotFoundException`.

### Pasos

En **ambos** handlers (`GetAllMedicalRecordsQueryHandler.cs`, `GetAllVaccinationsQueryHandler.cs`), reemplazar:

```csharp
var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(...);
var client = account is null ? null : await ...
```

Por:

```csharp
var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
    request.UserAccountId, cancellationToken)
    ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

var client = await unitOfWork.ClientsRepository.GetByUserIdAsync(
    account.UserId, cancellationToken);
```

Aplicar el mismo patrón en `GetMedicalRecordByIdQueryHandler` y `GetVaccinationByIdQueryHandler` si aún tratan `account is null` como staff.

### Verificación

Test con `UserAccountId` random → 404, no listado completo.

### Commit sugerido

`fix(clinical-history): fallar si la cuenta del JWT no existe en USER_ACCOUNTS`

---

## FASE 7 — P2: TOM-02 (PUT vacunas — scoping dueño)

### Problema

`GET /api/vaccinations/{id}` filtra por dueño; `PUT` no.

### Pasos

**1.** Agregar `Guid UserAccountId` a `UpdateVaccinationCommand`.

**2.** En `UpdateVaccinationCommandHandler`, **después** de cargar la vacuna, copiar el bloque de ownership de `GetVaccinationByIdQueryHandler` (líneas 18-36).

**3.** En `VaccinationsController` método `Update`, pasar `userAccountId` del JWT al command (igual que `GetById`).

### Verificación

Cliente intenta PUT sobre vacuna de otra mascota → **404**.

### Commit sugerido

`fix(vaccinations): aplicar scoping de dueño en PUT`

---

## FASE 8 — P2: VET-08 (paginación)

### Problema

`PaginatedResult<T>` y `PaginationMetadata` existen en `Application/Common/Models/` pero ningún listado operativo los usa.

### Pasos recomendados (empezar por uno)

**1.** `GET /api/appointments/me` (Fase 3) — mejor candidato porque el vet puede tener muchas citas.

**2.** En el repositorio, agregar overload con `Skip/Take` o método `GetByVeterinarianIdPagedAsync`.

**3.** Devolver:

```csharp
return new PaginatedResult<Appointment>(
    items,
    new PaginationMetadata(page, pageSize, totalItems, totalPages));
```

**4.** DTO de respuesta en API si hoy solo devuelves array plano (evitar romper FE: versión nueva o query `?page=` opcional).

### Commit sugerido

`feat(appointments): paginación en GET /api/appointments/me`

---

## Checklist final antes de merge

| # | Comprobación | Comando / acción |
|---|---|---|
| 1 | Build | `dotnet build src/Api/Api.csproj` |
| 2 | Tests | `dotnet test` (agregar tests nuevos de las fases 1-6) |
| 3 | Migraciones | No deberían hacer falta para estas fases (solo lógica). Si agregas índice único, evaluar migración EF |
| 4 | SEC-01 | Vet/Admin → `/appointments/mine` = 404 |
| 5 | Cliente | `/appointments/mine` = solo sus citas |
| 6 | Vet | `/veterinarians/me` y `/appointments/me` = 200 con datos coherentes |
| 7 | Estado cita | POST historial actualiza `APPOINTMENTS.STATUS_ID` en Oracle |
| 8 | Swagger | Descripciones de `/mine` actualizadas |

---

## Orden de implementación recomendado

```
Fase 1 (SEC-01)     → desbloquea FE cliente en /appointments/mine
Fase 2 (vet /me)    → desbloquea FE vet perfil
Fase 3 (citas /me)  → desbloquea FE vet agenda
Fase 4 (historial)  → desbloquea cambio de estado real
Fase 5 (medical)    → desbloquea atención / historia clínica segura
Fase 6 (TOM-01)     → endurecimiento seguridad
Fase 7 (TOM-02)     → consistencia PUT vacunas
Fase 8 (VET-08)     → mejora rendimiento (opcional)
```

---

## Fuera de alcance de esta guía (no mezclar en el mismo PR sin acuerdo)

| Tema | Responsable / nota |
|---|---|
| `RolesController` roto (módulo "Roles" no existe en `MODULES`) | Sahiam / líder — requiere fila en `MODULES` + `ROLE_PERMISSIONS` o revertir policy |
| SEC-02 reset contraseña ajena | Sahiam — mover a `SuperAdminOnly` + `PATCH /api/auth/me/password` |
| Catálogos (Especies, Servicios, etc.) | Gallo |
| Chat / Telegram | Sahiam |

---

## Contacto

Dudas de negocio (¿Admin puede crear historia sin ser vet?): escalar a la líder antes de implementar Fase 5.  
Dudas de datos demo (vet sin fila en `VETERINARIANS`): coordinar con Tomás para semilla operativa.

---

*Documento preparado por Tomás Medina para ejecución por Santiago Centeno. Basado en auditoría del 2026-09-01 sobre módulos operación Cliente/Veterinario.*
