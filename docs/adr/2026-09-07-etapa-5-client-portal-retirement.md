# ADR: Etapa 5 — retiro del portal Cliente JWT y contratos de agenda/OTP

- Estado: Aceptada (kickoff)
- Fecha: 2026-09-07
- Decisores: Equipo de desarrollo

## Contexto

Etapa 4 cerró `RegisterOwner` sin login de Cliente (`USER_ACCOUNTS` / `USER_CREDENTIALS` / password). El dueño no tiene panel web propio: interactúa por chatbot (Telegram). Aun así el API conserva rutas `[Authorize(Policy = ClientOnly)]` pensadas para un JWT de portal que **ya no es producto**.

Si las cinco tareas de Etapa 5 avanzan sin decisiones escritas, se pisan controllers, se inventa WhatsApp, se mezcla el OTP de citas con Gmail/Etapa 3, o se deja `RequesterPhoneNumber` divergente del teléfono del dueño registrado (rompe OTP anónimo de cancelar/reagendar).

Este kickoff **no** implementa los slices. Solo fija política, inventario y dueños de archivo.

## Decisiones

### 1. Política de teléfono en citas (dueño registrado)

Al **crear o editar** una cita ligada a un dueño con `Clients.PhoneNumber` persistido:

1. `RequesterPhoneNumber` se **normaliza** con la misma regla que el VO de citas / cliente (solo dígitos; longitud vigente 7–20).
2. Debe **coincidir** con `Clients.PhoneNumber` normalizado, **o** el backend lo **copia** desde el perfil y **ignora** un valor distinto del request (fuente de verdad = perfil).
3. Si el dueño aún no tiene teléfono en perfil, el request puede aportar uno solo cuando el caso de uso lo permita explícitamente; no se inventa un segundo teléfono “de cita” que contradiga el perfil una vez exista.
4. Staff (`CreateAppointment` / `UpdateAppointment`) y autoservicio legacy (`CreateMyAppointment`) comparten la regla; no hay excepción “el front manda otro número”.

Motivo: el OTP de acción de cita (`request-code` / `confirm-code`) autentica por teléfono de la cita. Si difiere del dueño, el chatbot no puede actuar con el número real del cliente.

### 2. Inventario objetivo ClientOnly — ideal cero JWT de portal

Objetivo de etapa: **cero rutas que exijan JWT rol Cliente** para un panel web. Por cada ruta actual la decisión es una de:

| Decisión | Significado |
|---|---|
| **Eliminar** | Quitar el endpoint del controller (breaking; solo si nadie lo consume ya). |
| **410 Gone** | Dejar el path respondiendo 410 + `code` estable (ver §6) durante una ventana corta; luego eliminar. |
| **Excepción temporal** | Solo con fecha/criterio de salida escrito. **Prohibido** usarla para reabrir portal Cliente. |

El canal real del dueño sigue siendo chatbot + staff. Lookup anónimo por teléfono (`GET /api/clients/by-phone/{phone}`) y OTP de citas **no** son portal JWT.

### 3. OTP de citas — anónimo + teléfono de la cita

| Ruta | Auth |
|---|---|
| `POST /api/appointments/mine/{id}/request-code` | `[AllowAnonymous]` + rate limit |
| `POST /api/appointments/mine/{id}/confirm-code` | `[AllowAnonymous]` + rate limit |

Reglas:

1. **No** es OTP Gmail / ContactVerification (Etapa 3).
2. **No** requiere JWT Cliente ni `ClientOnly`.
3. Sujeto = cita + acción (`Cancel` / `Reschedule`) + teléfono que **matchea** `RequesterPhoneNumber` de esa cita.
4. Quien retire `ClientOnly` en `MyAppointmentsController` **no** toca estos dos endpoints salvo para reforzar `AllowAnonymous` / tests.

Refuerza `docs/adr/2026-09-04-client-identity-and-otp-boundaries.md`.

### 4. Permisos seed del rol Cliente

1. Semilla de `ROLE_PERMISSIONS` (y equivalentes) para rol **Cliente**: **vacía o mínima de plataforma** — el dueño **no** “ve módulos” como staff en `GET /api/auth/permissions` ni en UI de escritorio.
2. No otorgar View/Create/Edit/Delete de módulos de escritorio al rol Cliente “para que /mine funcione”.
3. Si hace falta un claim técnico para tests legacy durante el retiro, debe morir con el 410/eliminación de esa ruta — no queda como producto.

### 5. WhatsApp — fuera de alcance

Prohibido en Etapa 5: DTOs, options, senders, seeds o “stubs por si acaso” de WhatsApp (contacto o citas). Canal de chatbot vigente: Telegram. Cualquier WhatsApp requiere ADR posterior.

### 6. Convención HTTP 410 / Gone

Cuando una ruta ClientOnly se retire con **410** (no eliminación inmediata):

1. **HTTP** `410 Gone`.
2. **Content-Type** `application/problem+json` (mismo patrón que auth / contact verification).
3. **`code` estable** (el front traduce por `code`, no por mensaje):
   - `ClientPortal.Gone` — ruta de portal Cliente retirada; usar chatbot/staff.
4. Cuerpo mínimo: `type`, `title` (`Gone`), `status` (`410`), `code`.
5. No reutilizar `Authentication.PlatformAccessDenied` (403) ni 404 para “ya no existe el portal”: 404 confunde con recurso inexistente; 403 implica “sigue existiendo pero no puedes”.

La excepción de Application / wiring en `GlobalExceptionHandler` es tarea de implementación (no de este kickoff). Hasta entonces, un action stub puede devolver el problema a mano **solo** si la tarea asignada lo define.

## Anexo A — rutas ClientOnly actuales (copiable)

Inventario verificado en código (2026-09-07). **No incluye** `GET /api/appointments/me` (veterinario + `RequirePermission("Citas", View)`).

```text
# Clients
GET    /api/clients/me

# Appointments — JWT portal (AppointmentsController)
GET    /api/appointments/mine
GET    /api/appointments/mine/{appointmentId}
GET    /api/appointments/booking/options
GET    /api/appointments/booking/slots
POST   /api/appointments/mine

# MyAppointments — JWT cancel (OTP aparte, anónimo)
PATCH  /api/appointments/mine/{id}/cancel

# Pets
GET    /api/pets/mine
POST   /api/pets/mine
PATCH  /api/pets/mine/{petId}

# Vaccinations
GET    /api/vaccinations/mine

# Account statements
GET    /api/accountstatements/mine
```

OTP (referencia; **no** son ClientOnly — no retirar como portal):

```text
POST   /api/appointments/mine/{id}/request-code   # AllowAnonymous
POST   /api/appointments/mine/{id}/confirm-code   # AllowAnonymous
```

## Anexo B — decisión por ruta + dueño de archivo (tarea 5.2)

Partir por **archivo**, no por orden de merge. Varias PRs en paralelo; un controller = un dueño.

| Slice | Archivo(s) | Rutas | Decisión kickoff |
|---|---|---|---|
| 5.2a | `Api/Clients/Controllers/ClientsController.cs` | `GET .../me` | **410** → luego eliminar |
| 5.2b | `Api/Appointments/Controllers/AppointmentsController.cs` | `mine`, `mine/{id}`, `booking/options`, `booking/slots`, `POST mine` | **410** → luego eliminar |
| 5.2c | `Api/Appointments/Controllers/MyAppointmentsController.cs` | solo `PATCH .../cancel` | **410** → luego eliminar; **no** tocar `request-code` / `confirm-code` |
| 5.2d | `Api/Pets/Controllers/PetsController.cs` | `GET/POST/PATCH .../mine*` | **410** → luego eliminar |
| 5.2e | `Api/Vaccinations/Controllers/VaccinationsController.cs` + `Api/AccountStatements/Controllers/AccountStatementsController.cs` | `GET .../mine` en ambos | **410** → luego eliminar |

Excepciones temporales: **ninguna** en kickoff. Si una integración externa aún llama una ruta, documentar salida en la PR del slice (fecha o issue), no ampliar el portal.

## Mapa de las cinco tareas (post-kickoff)

| Tarea | Qué hace | Qué no hace |
|---|---|---|
| **5.1** | Política teléfono create/update cita (Application + tests) | No retira ClientOnly; no WhatsApp |
| **5.2a–e** | 410/eliminación por archivo del Anexo B | No cambia OTP anónimo; no toca Etapa 3/4 |
| **5.3** | Seed permisos Cliente vacío/mínimo + tests de matriz | No reintroduce login Cliente |
| **5.4** | `Gone` / `ClientPortal.Gone` en pipeline de errores | No inventa códigos por endpoint distintos sin ADR |
| **5.5** | Quitar policy `ClientOnly` si ya no hay consumidores; cierre smoke etapa | No implementa panel web nuevo |

5.2a–e **no** se esperan entre sí: pueden mergearse en cualquier orden mientras no editen el mismo controller.

## Alternativas descartadas

- Mantener ClientOnly “por si el front lo usa”: contradice producto (sin panel Cliente) y Etapa 4 (sin account JWT Cliente).
- Convertir OTP de citas en JWT Cliente: rompe chatbot y el ADR de límites OTP.
- 404 o 403 genérico al retirar portal: códigos incorrectos para el front.
- WhatsApp stub en options/DTO: contaminaría Etapa 5 igual que en 3/4.

## Consecuencias

- El kickoff mergea solo docs (este ADR + enlace en CONTEXT).
- Las PRs 5.1–5.5 implementan sin reinterpretar alcance.
- CONTEXT §0 deja de decir “dejar ClientOnly quieto”: pasa a “Etapa 5 retira portal JWT según este ADR”.

## Criterios de aceptación del kickoff

- [x] ADR mergeado y enlazado desde CONTEXT.
- [x] Lista ClientOnly acordada (Anexos A–B).
- [x] OTP citas declarado anónimo (no Gmail, no JWT Cliente).
- [x] WhatsApp fuera de alcance (nadie lo implementa “por si acaso”).
- [x] Convención `410` + `ClientPortal.Gone` escrita.
- [x] Archivos asignados a 5.2a–e sin solapes de controller.

## Criterios de revisión (PRs posteriores)

- [ ] ¿Reintroduce JWT portal Cliente o seed de módulos de escritorio al rol Cliente?
- [ ] ¿Toca `request-code` / `confirm-code` sin mantener AllowAnonymous?
- [ ] ¿Declara WhatsApp o SMS de contacto?
- [ ] ¿Dos slices editan el mismo controller?
- [ ] ¿Usa 404/403 en lugar de 410 + `ClientPortal.Gone` al retirar portal?
- [ ] ¿Deja `RequesterPhoneNumber` distinto del teléfono normalizado del dueño registrado?
