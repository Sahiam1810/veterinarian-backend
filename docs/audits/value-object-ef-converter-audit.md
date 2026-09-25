# Auditoría de converters EF Core de Value Objects

| Campo | Valor |
|-------|--------|
| **Ticket** | 4 P2 — Backend — Auditoría de converters de Value Objects |
| **Fecha de auditoría** | 2026-09-25 |
| **Repositorio** | `backend` |
| **Base analizada** | punta de `origin/develop` al momento de la auditoría (`712854e`) |
| **Alcance** | Solo lectura de código y configuración EF; inventario, riesgo, decisiones y plan |
| **Fuera de alcance de esta fase** | Cambios de runtime, dominio, EF, migraciones, tolerancia adicional, SQL, Producción |

---

## 1. Propósito y reglas

### 1.1 Riesgo de materialización

En EF Core, un mapeo del tipo:

```csharp
.HasConversion(
    value => value.Value,
    str => SomeValueObject.Create(str))
```

ejecuta el conversor **DB → dominio en cada materialización** de la entidad (listados `ToListAsync`, `Include`, get-by-id, etc.). Si `Create` lanza (`ArgumentException` por blank, formato, rango o longitud), **toda la consulta falla**: no se omiten filas; el listado completo deja de responder.

Ese fallo es especialmente grave en endpoints de alto tráfico que materializan la entidad completa **sin proyección** a DTO en el repositorio.

### 1.2 Separación escritura / lectura

| Camino | Regla |
|--------|--------|
| **Escritura / API** | `Create` / `TryCreate` (y validadores FluentValidation) siguen **estrictos**. No se relajan por datos históricos. |
| **Lectura (EF)** | Una estrategia tolerante (`CreateFromPersistence` u equivalente) **solo** con evidencia de corrupción real y decisión explícita documentada (típicamente **B** o **D**). |

### 1.3 Prohibiciones

- No inventar **fallbacks** (teléfonos sintéticos, emails placeholder, truncados silenciosos, valores “default” de catálogo).
- No **ocultar excepciones** de forma global (p. ej. `GlobalExceptionHandler` que trague fallos de materialización).
- No **excluir registros** de listados por defecto ante filas corruptas (decisión **E** solo con diseño explícito de observabilidad; no es el default).
- No volver **opcional** un campo requerido en dominio/DTO/esquema sin decisión de producto y, si aplica, cambio de schema aprobado.
- No modificar Value Objects “por precaución” sin evidencia.

### 1.4 Límite de evidencia de datos

Esta auditoría **no consultó Oracle ni Producción**. Las conclusiones sobre nulabilidad y riesgo se basan en **código, configuraciones EF, snapshot/migraciones y consumidores API**. La **presencia real de corrupción** es una hipótesis a confirmar con muestreo sanitizado o consulta de auditoría aprobada antes de remediación.

---

## 2. Método y alcance

### 2.1 Búsquedas realizadas

En `backend`:

- `.HasConversion(` / `HasConversion(`
- `ValueConverter<` / `ValueComparer<` (sin clases dedicadas encontradas)
- `Create(`, `TryCreate(`, `CreateOptional(`, `CreateFromPersistence`
- `*Configuration.cs` / `IEntityTypeConfiguration`
- Consumidores: repositorios `GetAll*`, handlers, controllers, mappers DTO, `AsNoTracking` / `Include`

### 2.2 Configuraciones EF auditadas

Todas las `IEntityTypeConfiguration` bajo `src/Infrastructure/**/Configuration`, incluyendo (no exhaustivo de nombres de archivo): Clients, Pets, Appointments, Availabilities, Species, Races, Roles, Users, Notifications, Specialties, Services, SenderTypes, EscalationStatuses, Modules, ClientsPets, Chat*, Telegram*, Medication/Procedure Orders, Hospitalization, etc.

No hay convenciones globales de VO en `OnModelCreating` que registren converters adicionales.

### 2.3 Criterio de priorización

1. Endpoints de **listado** de alto uso.
2. Materialización de **entidad completa** (sin `Select` a DTO en repositorio).
3. Uso de **`Include`** que arrastra entidades con VO converters.
4. Mapping a DTO **después** de materializar (los converters ya corrieron).

Clasificación:

| Nivel | Significado |
|-------|-------------|
| **P0** | Puede tumbar listado de alto tráfico / crítico. |
| **P1** | Puede tumbar listado o detalle frecuente. |
| **P2** | Detalle o flujo administrativo poco frecuente. |
| **P3** | No expuesto o riesgo despreciable / no lanza. |

### 2.4 Módulos explícitamente revisados

Clientes, Mascotas, Especies, Razas, Roles, Servicios, Órdenes (medicación/procedimientos), Citas, Chat/Telegram, Notificaciones, Direcciones, Correos e Identificaciones.

---

## 3. Inventario completo

### 3.1 Converters de Value Objects (alcance principal)

**Conteo:** 26 propiedades con conversión DB→dominio vía Value Object (incluyendo `PrimaryOwner` y el teléfono ya tolerante).

| Entidad | Propiedad | Tabla/columna | Tipo / Value Object | Conversión DB→dominio | Puede lanzar | Nulabilidad/modelo | Endpoints o consumidores | Riesgo | Decisión |
|----------|-----------|---------------|---------------------|------------------------|--------------|--------------------|---------------------------|--------|----------|
| Client | FullName | CLIENTS.FULL_NAME | ClientFullName | `Create(str)` | Sí (blank / >150) | CLR required; DB NOT NULL VARCHAR2(150) | `GET /api/clients`, lookup, Includes ClientPet (pending/admission/chat) | P0 | A / D si evidencia |
| Client | Email | CLIENTS.EMAIL | ClientEmail | `Create(str)` | Sí (blank / formato / >150) | Required; unique | Listado clientes y cascadas con Client | P0 | A / D si evidencia |
| Client | IdentificationNumber | CLIENTS.IDENTIFICATION_NUMBER | ClientIdentificationNumber | `Create(str)` | Sí (blank / >20) | Required; unique | Listado + by-identification | P0 | A / D si evidencia |
| Client | Address | CLIENTS.ADDRESS | ClientAddress | `Create(str)` | Solo si >150; blank → VO con null | Optional | DTO completo de clientes | P2 | A (D si over-length confirmado) |
| Client | PhoneNumber | CLIENTS.PHONE_NUMBER | ClientPhoneNumber | **`CreateFromPersistence(str)`** | **No** (inválido/blank → null) | CLR `ClientPhoneNumber?`; Fluent `IsRequired(false)`; **Oracle/snapshot NOT NULL** | Listados/Includes; escritura/API sigue `Create`/`TryCreate` | Mitigado | **A** (ya corregido) |
| Pet | Name | PETS.NAME | PetName | `Create(str)` | Sí | Required | `GET /api/pets`, bot pets, pending, admission | P0 | A / D si evidencia |
| Pet | Gender | PETS.GENDER | PetGender | `Create(str)` | Sí (solo M/F) | Required NVARCHAR2(1) | Igual Name | P0/P1 | A / **D** si evidencia |
| Pet | Weight | PETS.WEIGHT | PetWeight | `Create(decimal)` | Sí (fuera 0.01–500) | Required NUMBER(6,3) | Igual Name | P0/P1 | A / **D** si evidencia |
| Pet | Observations | PETS.OBSERVATIONS | PetObservations | `Create(str)` | Solo >500 | Optional | Listado pets | P2 | A |
| Pet | PhotoUrl | PETS.PHOTO_URL | PetPhotoUrl | `Create(str)` | Sí (no http/https) | Optional (`_photoUrl`); DB nullable | `GET /api/pets`, bot pets | P0/P1 | **B candidato** (evidencia) |
| Appointment | RequesterPhoneNumber | APPOINTMENTS.REQUESTER_PHONE_NUMBER | RequesterPhoneNumber | blank → null; else `Create` | Sí si no-blank inválido | Optional; DB nullable | `GET /api/appointments*` (siempre materializa) | P1 | **B candidato** (evidencia) |
| Appointment | BookingRequestKeyHash | APPOINTMENTS.BOOKING_REQUEST_KEY_HASH | BookingRequestKeyHash | blank → null; else `Create` | Sí (≠64 hex) | Optional; unique filtrado | Materialización de citas | P1/P2 | **D** si evidencia (idempotencia) |
| Species | Name | SPECIES.NAME | SpeciesName | `Create(str)` | Sí (blank / >20) | Required; MaxLength=columna | `GET /api/species`; Include desde races/pets | P1 | A / D si blank |
| Race | Name | RACES.NAME | RaceName | `Create(str)` | Sí (blank / >20) | Required; unique con SpeciesId | `GET /api/races` (+ Include Species) | P1 | A / D si blank |
| Role | Name | ROLES.NAME | RoleName | `Create(str)` | Sí | Required; unique | `GET /api/Roles`, joins users | P2 | A (seed) |
| User | Email | USERS.EMAIL | UserEmail | `Create(str)` | Sí (formato) | Required; unique | `GET /api/users`, auth, Includes | P1 | A / D si evidencia |
| User | PhotoUrl | USERS.PHOTO_URL | UserPhotoUrl | `Create(str)` | Sí (URL inválida) | Optional; DB nullable | Users / `GET /api/auth/me` | P1 | **B candidato** (evidencia) |
| Notification | Message | NOTIFICATIONS.MESSAGE | NotificationMessage | `Create(str)` | Sí | Required VARCHAR2(1000) | `GET /api/notifications*` | P1 | A / D si evidencia |
| Notification | Status | NOTIFICATIONS.STATUS | NotificationStatus | `Create(str)` | Sí | Required | Igual | P1 | A / D si evidencia |
| Notification | Type | NOTIFICATIONS.TYPE | NotificationType | `Create(str)` | Sí | Required | Igual | P1 | A / D si evidencia |
| Specialty | Name | SPECIALTIES.NAME | SpecialtyName | `Create(str)` | Sí | Required; unique | Catálogo / create user | P2 | A |
| Specialty | Description | SPECIALTIES.DESCRIPTION | SpecialtyDescription | `Create(str)` | Solo >120; blank OK | Optional | Catálogo | P2 | A |
| SenderType | Name | SENDER_TYPES.NAME_TYPE | SenderTypeName | `Create(str)` | Sí | Required | Chat (catálogo) | P2 | A |
| EscalationStatus | Name | ESCALATIONS_STATUSES.NAME_STATUS | EscalationStatusName | `Create(str)` | Sí | Required | Escalaciones | P2 | A |
| Module | Name | MODULES.NAME | ModuleName | `Create(str)` | Sí | Required; unique | Auth / permissions | P2 | A |
| ClientPet | IsPrimaryOwner | CLIENT_PETS.IS_PRIMARY_OWNER | PrimaryOwner | `Create(bool)` vía Y/N | **No** | Required CHAR(1) | Listados ClientPet | P3 | A |

**Métodos de lectura segura existentes (referencia):**

- `ClientPhoneNumber.CreateFromPersistence` / `TryCreate` / `CreateOptional` — solo el primero se usa en converter EF de lectura.
- `ConsultingRoom` / `ShiftName.CreateOptional` — usados en entidad/comandos; **no** como converter EF (columnas string plain en Availability/Appointment).

### 3.2 Convertidores no-VO (riesgo lateral; fuera del alcance principal)

Estos **no** son Value Objects de dominio. Se inventarían solo para no ocultar riesgo lateral; **no** se remedia en este ticket sin evidencia/ticket separado.

| Tipo | Dónde (ejemplos) | DB→CLR | Puede lanzar | Notas | Decisión en este ticket |
|------|------------------|--------|--------------|-------|-------------------------|
| `Guid.Parse` | Casi todas las PK/FK `VARCHAR2(36)` (Clients, Pets, Appointments, Orders, Chat*, Users, Roles, …) | string → Guid | Sí si GUID corrupto | Corrupción estructural; no validación de VO | Fuera de alcance; auditoría/ticket aparte |
| `TimeOnly.Parse` | AVAILABILITIES.START_TIME / END_TIME | string → TimeOnly | Sí si formato inválido | Puede tumbar listados de agendas | Fuera de alcance; ticket aparte |
| `bool` ↔ `'Y'/'N'` | Services.IsActive, Availability.IsActive, ClientPet primary owner mapping | char/bool | No por validación de dominio | — | A |
| `HasConversion<int>()` / bool↔int | Clients.IsActive, Users.IsActive, Appointments.IsPaid, etc. | int/bool | No típico | — | A |
| DayOfWeek / enums numéricos | Availability.DayOfWeek | int | Bajo | — | A |

**Módulos sin VO Create en strings de negocio (evidencia de búsqueda):**

| Módulo | Evidencia |
|--------|-----------|
| **Servicios** | `ServiceConfiguration`: Guid + bool Y/N; `Name`/`Price`/`Duration` son tipos CLR plain |
| **Órdenes** (Medication/Procedure + items) | Solo Guid (+ null-safe Guid opcional); riesgo **indirecto** por `Include` Client+Pet en `/pending` |
| **Chat messages / conversations / participants / escalations (IDs)** | Principalmente Guid; nombres de catálogo en SenderType / EscalationStatus (tabla VO arriba) |
| **TelegramUserLink** | Guid.Parse (nullable Guid con null-check) |

---

## 4. Matriz de decisiones

### 4.1 Definiciones

| Código | Significado |
|--------|-------------|
| **A** | Sin cambio / validar con evidencia. Constraints y código hacen improbable el fallo, o el converter no lanza. |
| **B** | Lectura tolerante a null **solo** si el campo es opcional semánticamente y modelo/DTO/schema permiten representar ausencia **sin inventar valor**. Escritura sigue estricta. |
| **C** | Fallback **explícito ya definido en el dominio**; no inventar uno nuevo. *(En esta auditoría no se identificó un C aplicable a converters pendientes.)* |
| **D** | Corrección SQL obligatoria: identidad, autorización, FK/clave, invariante esencial; no ocultar. |
| **E** | Exclusión explícita de listado + métrica/log; **no es el default** y no se recomienda como estrategia general. |

### 4.2 Decisiones por converter (resumen del informe)

| Converter | Decisión | Notas |
|-----------|----------|-------|
| **ClientPhoneNumber** | **A (ya corregido)** | `CreateFromPersistence`: blank/inválido histórico → `null` solo en lectura. Create/Update/API/`TryCreate` estrictos. **Oracle `PHONE_NUMBER` permanece NOT NULL**; no migration a nullable por el fix CLR. Tests VO + SQLite + HTTP. |
| ClientFullName | A; **D** si corrupción | No nullable semántico; no inventar nombre. |
| ClientEmail | A; **D** si corrupción | Regex más estricta que “cualquier string”; required en entidad/DTO → **no B**. |
| ClientIdentificationNumber | A; **D** si corrupción | Clave de negocio / unique → **no B**. |
| ClientAddress | A; D si over-length | Blank ya seguro; truncar sería inventar. |
| PetName | A; D si corrupción | Required. |
| PetGender | A; **D** si corrupción | Enum-like M/F; no inventar. |
| PetWeight | A; **D** si corrupción | Rango de dominio; no inventar. |
| PetObservations | A | Riesgo length bajo. |
| **PetPhotoUrl** | **B candidato** | Opcional; análogo al patrón teléfono; **solo con evidencia**. |
| **RequesterPhoneNumber** | **B candidato** | Ya null si blank; falta tolerancia a no-blank inválido; **solo con evidencia**. |
| BookingRequestKeyHash | **D** si evidencia | Idempotencia; no nullificar como default. |
| SpeciesName / RaceName | A; D si blank | MaxLength alineado a columna. |
| RoleName / ModuleName / Specialty* / SenderTypeName / EscalationStatusName | A | Catálogos/seed; P2. |
| UserEmail | A; D si corrupción | Como ClientEmail. |
| **UserPhotoUrl** | **B candidato** | Opcional; **solo con evidencia**. |
| Notification Message/Status/Type | A; D si corrupción | Required. |
| PrimaryOwner | A | No lanza. |

**Clarificaciones obligatorias:**

- No hay recomendación **E** por defecto.
- No hay **C** pendiente (no hay fallback de dominio reutilizable para email/nombre/género/peso/hash).
- Email, identificación, nombres, género, peso, roles, módulo, hashes e integridad **no** reciben tolerancia automática; ante corrupción confirmada la vía normal es **D**.
- `PetPhotoUrl`, `UserPhotoUrl` y `RequesterPhoneNumber` son **candidatos B**, no cambios aprobados.

---

## 5. Endpoints prioritarios

Patrón transversal observado: repositorios devuelven **entidades completas** (`AsNoTracking` + `ToListAsync` en listados); el DTO se arma **después**. Una fila con valor que haga fallar `Create` tumba **toda** la consulta.

| Prioridad | Endpoint / consulta | Converters materializados | Por qué importa |
|-----------|---------------------|---------------------------|-----------------|
| **P0** | `GET /api/clients` → `ClientRepository.GetAllAsync` | FullName, Email, Identification, Address; Phone **tolerante** | Listado staff; una email/nombre/id inválidos → 500 global |
| **P0** | `GET /api/pets` → `PetRepository.GetAllAsync` | Name, Gender, Weight, Observations, PhotoUrl | Listado mascotas; PhotoUrl/Gender/Weight pueden corromper materialización |
| **P0** | Orders `GET …/pending` (medication/procedure) | Include ClientPet→Client + Pet | Cascada de VOs de Cliente y Mascota aunque el DTO solo muestre nombres |
| **P0/P1** | `GET /api/chat/conversations` + enrichment N+1 `Clients.GetById` | VOs Client por conversación | Un cliente corrupto puede tumbar el listado enriquecido |
| **P1** | `GET /api/appointments` (+ me / detail / receipt / bot) | RequesterPhoneNumber, BookingRequestKeyHash; detail incluye Pet/User | Toda cita materializa teléfono solicitante |
| **P1** | `GET /api/species`, `GET /api/races` | SpeciesName; RaceName (+ Species) | Catálogos UI mascotas |
| **P1** | `GET /api/users`; notifications con Include User | UserEmail, UserPhotoUrl | Staff / notificaciones |
| **P1** | `GET /api/notifications*` | Message, Status, Type | Listado completo de notificaciones |
| **P2** | Roles, Modules, Specialties, SenderTypes, EscalationStatuses | Name.Create (y Description specialty) | Admin / catálogos pequeños |
| **P3** | ClientPet sin Include de Client/Pet | PrimaryOwner (no lanza); Guids | Bajo para VO |

**Hospitalization admission-options** y similares: Include Client+Pet → mismo riesgo de cascada P0/P1 aunque el DTO proyecte solo nombres.

---

## 6. Cobertura actual y huecos

### 6.1 Existente (teléfono cliente — precedente)

| Capa | Test / artefacto |
|------|------------------|
| VO | `ClientPhoneNumberTests` — `TryCreate`, `CreateFromPersistence` |
| Materialización EF (SQLite in-memory) | `ClientPhoneHistoricalReadToleranceTests` — insert crudo inválido + `GetAllAsync` |
| HTTP listado | `ClientPhoneHistoricalReadHttpTests` |

Config: `ClientConfiguration` usa `CreateFromPersistence`; comentarios documentan CLR nullable vs Oracle NOT NULL.

### 6.2 Huecos

- Sin pruebas de **materialización EF** con filas históricas inválidas para: Client Email / FullName / Address; Pet*; Appointments RequesterPhone / BookingHash; Species/Race; User Email/Photo; Notifications.
- Tests unitarios parciales de algunos VOs (p. ej. ClientFullName/Email, ClientAddress, BookingRequestKeyHash) **no** cubren el camino EF.
- **No** crear suites genéricas “para todos los converters” antes de decidir **un** converter concreto a remediar.

### 6.3 Patrón de prueba propuesto (cuando haya evidencia y decisión)

1. SQLite in-memory + `EnsureCreated`.
2. `ExecuteSqlRaw` insertando el string/número **crudo** (bypass del converter de escritura).
3. Llamar el **repositorio/listado real** (`GetAllAsync` u equivalente).
4. Assert: filas no se pierden; campo opcional → null (si **B**) o la corrección SQL se valida en otro pipeline (si **D**).
5. Si viable: test HTTP del listado afectado (factory existente o mínimo).

---

## 7. Plan de remediación por evidencia

Protocolo obligatorio antes de tocar dominio/EF:

1. **Obtener** muestra sanitizada o consulta de auditoría **aprobada** (no Prod improvisado desde el agente).
2. **Confirmar** qué `Create` falla y en **qué endpoint** se materializa.
3. **Clasificar** el campo: opcional semántico (candidato **B**) vs invariante esencial / identidad / autorización (**D**).
4. **Elegir B o D**, documentar la decisión (actualizar esta auditoría o ADR breve).
5. **Aplicar cambio mínimo** + tests de materialización/listado del converter tocado.
6. **Mantener escritura estricta** (`Create`/`TryCreate`/validators).
7. **Verificar** que no se ocultan listas ni se inventan valores; no usar **E** como atajo.
8. Si **D** (SQL correctivo): preview de filas afectadas, backup, criterio inequívoco, script **idempotente**, validación posterior (re-lectura / smoke del listado).

---

## 8. Fuera de alcance

- Cambiar todos los Value Objects “por precaución” sin evidencia.
- Modificar `GlobalExceptionHandler` u ocultamiento global de excepciones de materialización.
- Migraciones o nulabilidad general de columnas (incluido volver `PHONE_NUMBER` NULL por el fix de lectura).
- Fallbacks ficticios o limpieza de datos sin criterio.
- Cambios en **frontend** / **chatbot**.
- Remediar `Guid.Parse` / `TimeOnly.Parse` sin ticket/evidencia **separados**.
- Seeds, Docker, `.env`, dependencias, deploy, Telegram, VPS, Producción desde esta línea de trabajo documental.

---

## 9. Próximas prioridades

> **No son cambios aprobados.** Son prioridades de **investigación** sujetas a evidencia real de datos.

1. **ClientEmail / ClientFullName / ClientIdentificationNumber** — impacto P0 en listado clientes y cascadas; si se demuestra corrupción → normalmente **corrección SQL (D)**, no tolerancia.
2. **PetPhotoUrl / UserPhotoUrl** — candidatos **B** si hay URLs históricas no http(s).
3. **RequesterPhoneNumber** — candidato **B** si hay no-blank inválidos en citas.
4. **PetGender / PetWeight** — ante corrupción → **D** (SQL), no inventar género/peso.
5. **Guid.Parse / TimeOnly.Parse** — auditoría o ticket **separado**.

---

## 10. Confirmaciones de la fase documental

| Ítem | Estado |
|------|--------|
| Documento versionado | Este archivo |
| Cambios runtime / dominio / EF / migrations / tests | **Ninguno** en esta fase |
| Consulta Oracle / Producción / SQL correctivo | **No** |
| Git commit / push / PR | **Pendiente de autorización** |

---

## Referencias de código (lectura)

- `src/Infrastructure/Clients/Configuration/ClientConfiguration.cs` — precedente `CreateFromPersistence`
- `src/Domain/Clients/ValueObjects/ClientPhoneNumber.cs`
- `src/Infrastructure/Pets/Configuration/PetConfiguration.cs`
- `src/Infrastructure/Appointments/Configuration/AppointmentConfiguration.cs`
- `tests/Infrastructure.Tests/Clients/ClientPhoneHistoricalReadToleranceTests.cs`
- `src/Infrastructure/Migrations/VeterinaryDbContextModelSnapshot.cs` — nulabilidad snapshot (p. ej. `PHONE_NUMBER` IsRequired)
