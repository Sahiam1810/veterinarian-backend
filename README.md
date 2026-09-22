# Huellitas — Veterinarian Backend

API de Huellitas para la operación de una clínica veterinaria. Centraliza la gestión de personal, dueños, mascotas, agenda, atención clínica, catálogos, notificaciones y la integración con el chatbot.

Está construida con ASP.NET Core 10, Oracle y EF Core, y está organizada en Domain, Application, Infrastructure y Api.

> El portal web es solo para el personal de la clínica. Los clientes no tienen contraseña, cuenta de plataforma ni inicio de sesión web: usan el chatbot de Telegram, que identifica al dueño pidiéndole nombre, cédula, correo y teléfono directamente en la conversación cuando la operación requiere datos privados. Por decisión de negocio, ningún canal envía ni valida un código de verificación para esto.

## Capacidades

- Gestión de personal, roles, permisos por módulo y cuentas internas.
- Dueños (solo `CLIENTS`, sin `userId`), mascotas, especies, razas, especialidades, servicios y diagnósticos.
- Agenda, disponibilidad, ausencias, prevención de solapamientos, citas, historia clínica, vacunas y recordatorios.
- Notificaciones en tiempo real mediante SignalR (`/hubs/notifications`).
- JWT RS256 con access token, refresh token rotativo y permisos como claims.
- Integración opcional con el servicio de agente conversacional, SMTP, Twilio y Telegram.
- Verificación de correo por OTP: (a) opcional en alta de dueño hecha por
  **staff** cuando `RegisterOwner__RequireContactProofs=true`; (b) **Claim**
  cuando un cliente ya registrado escribe desde otro Telegram (correo
  resuelto por el servidor). El alta desde el bot de un cliente **nuevo** y
  el `bot-link` inmediato **no** exigen OTP.
- Escalamiento de conversaciones de Telegram a un asesor humano: detección de frase, `ChatEscalation`, notificación a la bandeja de Recepcionista y reenvío de sus respuestas de vuelta al chat (ver «Escalamiento a un asesor humano» más abajo).
- Runtime conversacional operativo: conversaciones, mensajes, escalaciones, participantes (con `clientId`) y agentes humanos. Fuera de producto (D1–D4): estados de cuenta, catálogos/ejecuciones de IA, adjuntos de chat y `link-codes`/registro web Telegram.
- Swagger en Development, rate limiting, CORS, logging estructurado con Serilog y respuestas de error `application/problem+json`.
- Recordatorios de citas por Telegram (1 hora antes) con configuración separada de los recordatorios de 24h.

## Arquitectura

| Proyecto | Responsabilidad |
| --- | --- |
| `src/Domain` | Entidades, value objects y reglas de negocio sin dependencias de infraestructura. |
| `src/Application` | Casos de uso con MediatR, validaciones FluentValidation y contratos. |
| `src/Infrastructure` | EF Core/Oracle, repositorios, seguridad, correo, OTP, Telegram y servicios de fondo. |
| `src/Api` | Controladores HTTP, autenticación, autorización, Swagger, CORS, SignalR y adaptadores de integración. |

```text
Frontend web (solo staff) ─┐
                           ├── API .NET ─── Oracle
Telegram ─── Bot ──────────┘       │
                                    └── servicio de agente conversacional (opcional)
```

El agente no accede a Oracle directamente. El backend aplica las reglas de negocio, la propiedad de los datos y la disponibilidad de agenda antes de registrar una operación.

## Acceso: personal y dueños

### Personal de la clínica

Las cuentas internas inician sesión en `POST /api/auth/login` con correo y contraseña. La API emite access token y refresh token; `POST /api/auth/refresh` rota el refresh token. Los permisos efectivos se calculan en Oracle al iniciar o renovar sesión y se incluyen en el JWT. `SuperAdmin` es un rol persistido, no una variable de entorno.

Los endpoints administrativos usan permisos dinámicos por módulo (`Clientes`, `Mascotas`, `Citas`, `Usuarios`, `Reportes`, etc.) y las políticas de rol necesarias. Consulte Swagger para el requisito exacto de cada ruta.

### Dueños: sin contraseña ni cuenta de plataforma

Un **cliente** (dueño de mascota) vive solo en `CLIENTS`: nombre, correo, cédula,
teléfono y dirección. **No** tiene fila en `USERS`, no tiene contraseña y
**nunca** inicia sesión web. El personal gestiona dueños con `GET`/`POST`/`PUT`
`/api/clients` (sin `userId` ni `registrationDate`).

El flujo del chatbot es:

1. En Telegram, las consultas generales funcionan en modo invitado si
   `Telegram__GuestModeEnabled=true`.
2. Cuando hace falta identidad (agendar cita, registrar mascota), el chatbot
   pide nombre, cédula, correo y teléfono. Un **cliente nuevo** se registra y
   se vincula **sin OTP**.
3. Busca por cédula en `GET /api/clients/by-identification/{identificationNumber}`
   (`{ id, identificationNumber, createdAt }`). Si no existe, alta con
   `POST /api/owners/bot` → **201** `{ clientId }` (solo inserta en `CLIENTS`).
4. Vincula Telegram con `POST /api/integrations/telegram/bot-link` y body
   `{ clientId }` (token de invitado con `telegram_user_id`).
5. Si **ya es cliente** pero escribe desde **otro** Telegram, no use
   `bot-link` directo: pida OTP al correo registrado vía
   `POST /api/contact-verification/email/request-claim-by-identification`,
   confirme con `email/confirm` y cierre con
   `POST /api/integrations/telegram/bot-link/claim` (`{ sessionId, proof }`).
   Detalle: [contrato v2 §9.1](docs/contracts/clientes-usuarios-api-v2.md) y
   [guía Telegram](docs/integrations/telegram.md).

Las rutas del portal JWT de cliente que siguen expuestas (`/api/pets/mine`,
`/api/appointments/mine`, `booking/options`, `booking/slots`, etc.) responden
`410 Gone`; `/api/clients/me` y el OTP de citas ya no existen. El bot usa
`/api/bot/pets` y `/api/bot/appointments` con JWT delegado interno
(`TelegramAgent`).

### Escalamiento a un asesor humano

Cuando un cliente **ya vinculado** escribe una frase de escalamiento ("asesor", "hablar con alguien",
"hablar con un humano", etc.), el backend crea un `ChatEscalation` en estado Pendiente para su
conversación y responde con un mensaje fijo, sin llamar al agente de IA en ese turno. Un
**invitado sin vincular** que escriba lo mismo nunca escala directamente: se le redirige a decir
qué necesita (agendar cita, registrar mascota) para que el flujo de identificación descrito arriba
lo reconozca por su propia cuenta primero.

Mientras una conversación está escalada, el agente de IA se mantiene en silencio: cada mensaje que
llega al backend reenvía al chatbot el estado `isEscalated=true`, y el chatbot responde sin
generar texto. La conversación queda visible en la bandeja de Recepcionista, donde un asesor
humano responde manualmente; esas respuestas (`POST /api/chat/messages` con
`SenderTypesId = Agente humano`) se reenvían automáticamente al mismo chat de Telegram. Al crear
la resolución del escalamiento (`POST /api/chat/escalation-resolutions`), el backend también
actualiza `ChatEscalation.EscalationStatusId` a Resuelta, para que la bandeja deje de mostrarlo
como pendiente.

## Requisitos

- .NET SDK 10.
- Una instancia Oracle accesible. El proveedor se configura con compatibilidad Oracle 21c.
- Acceso a `sqlplus` si se aplicarán los seeds provistos.
- Un par RSA de al menos 2048 bits, en PEM y codificado en Base64, para JWT.
- Opcional: bot de Telegram, SMTP y el repositorio/servicio del agente conversacional.

La versión de `dotnet-ef` está fijada en `dotnet-tools.json`.

## Configuración local

La API carga un archivo `.env` buscando desde el directorio de ejecución hacia arriba y, después, incorpora las variables de entorno del proceso. Los valores del proceso prevalecen. Use `.env.example` como plantilla: copie a `.env` (ignorado por Git) y complete secretos localmente. No suba `.env` reales.

Como mínimo, la aplicación exige Oracle, CORS y JWT. Un ejemplo de estructura es el siguiente; reemplace todos los marcadores por valores reales.

```dotenv
ConnectionStrings__DefaultConnection=User Id=VET_APP;Password=<secret>;Data Source=//localhost:1522/FREEPDB1

Cors__AllowedOrigins__0=https://huellitas.chatcampuslands.com
# Local: Cors__AllowedOrigins__1=http://localhost:5174

Jwt__Issuer=huellitas-api
Jwt__Audience=huellitas-web
Jwt__KeyId=<identificador-de-clave>
Jwt__PrivateKeyPemBase64=<PEM-privado-en-Base64>
Jwt__PublicKeyPemBase64=<PEM-publico-en-Base64>
Jwt__AccessTokenMinutes=15
Jwt__RefreshTokenDays=7
Jwt__ClockSkewSeconds=60

# Integraciones desactivadas mientras no se configuren.
Agent__Enabled=false
Telegram__Enabled=false
Email__Enabled=false
Twilio__Enabled=false
```

Las claves privadas, tokens, contraseñas Oracle, credenciales SMTP, OTP y datos personales no deben subirse al repositorio ni imprimirse en logs.

### Docker

- **Producción (stack completo):** use `deploy/docker-compose.prod.yml` y la guía `deploy/DEPLOY.md`. Red `huellitas_network`, DNS `backend` / `chatbot` / `oracle`, API solo en `127.0.0.1:5233`. `Agent__BaseUrl=http://chatbot:8010`. Connection string con `Data Source=//oracle:1521/FREEPDB1` y credenciales solo en `.env` del VPS.
- **Local:** `docker-compose.yml` en este repo (oracle + backend, red `huellitas-internal`). Copie `.env.example` → `.env` e incluya `ORACLE_PASSWORD`, `APP_USER`, `APP_USER_PASSWORD` y `ConnectionStrings__DefaultConnection` sin hardcodear passwords en el YAML.
- Migraciones EF y seeds son **manuales** en el primer deploy (no las ejecuta Compose). Ver sección «Base de datos y datos iniciales» y `deploy/DEPLOY.md`.

### Opciones de negocio disponibles

`src/Api/appsettings.json` aporta los valores no secretos por defecto:

| Sección | Valores relevantes por defecto |
| --- | --- |
| `AppointmentBooking` | Zona `America/Bogota`, 60 minutos mínimos de anticipación y 30 días máximos. |
| `ContactVerification` | OTP de correo: 10 minutos, 5 intentos, reenvío a los 60 s y proof válido 15 minutos. |
| `RegisterOwner` | `RequireContactProofs=false` por defecto; controla si el alta hecha por staff exige proof de contacto. El alta desde bot y desde Telegram **nunca** exige proof, sin importar este valor. |
| `RateLimiting` | Límite global y límites específicos para login, refresh, Telegram, lookups y OTP. |
| `Reminders` | Worker de recordatorios activo por defecto; ventana y frecuencia configurables. |
| `TelegramReminders` | Worker de aviso Telegram al dueño ~1 h antes; requiere `Telegram:Enabled=true`. Ventana 50–70 min, sondeo cada 5 min, estados `AGENDADA` y `CONFIRMADA`. |
| `TelegramReminders__LeadMinutes` | Tiempo de anticipación para el recordatorio (60 minutos por defecto). |
| `TelegramReminders__GraceMinutes` | Margen de tolerancia alrededor del tiempo objetivo (10 minutos por defecto). |
| `TelegramReminders__PollIntervalMinutes` | Frecuencia de sondeo del worker (5 minutos por defecto). |
| `TelegramReminders__AllowedStatusNames` | Estados de cita elegibles para recordatorio (AGENDADA,CONFIRMADA por defecto). |

Puede sobrescribir cualquier valor con el formato `Seccion__Propiedad` en `.env` o en el entorno de despliegue.

### SMTP, verificación de contacto y Twilio

Para enviar OTP por correo habilite y complete:

```dotenv
Email__Enabled=true
Email__Host=<smtp-host>
Email__Port=587
Email__Username=<smtp-user>
Email__Password=<smtp-secret-o-app-password>
Email__FromAddress=no-reply@tu-dominio.example
Email__FromName=Huellitas
Email__UseTls=true
```

`ContactVerification__OtpPepperBase64` es opcional si ya se suministra un pepper en `Telegram__OtpPepperBase64`. La sección `AppointmentVerification__*` se retiró: si tu entorno tenía el pepper solo en `AppointmentVerification__OtpPepperBase64`, muévelo a `ContactVerification__OtpPepperBase64`. Cuando se habilita Telegram, su pepper es obligatorio. Twilio es opcional y requiere `Twilio__AccountSid`, `Twilio__AuthToken` y `Twilio__FromNumber` además de `Twilio__Enabled=true`.

### Servicio del agente

La integración está deshabilitada por defecto. Para activarla se requieren los siguientes valores:

```dotenv
Agent__Enabled=true
Agent__BaseUrl=http://127.0.0.1:8010
Agent__MessagesPath=/api/v1/messages
Agent__RequestTimeoutSeconds=90
Agent__MaxResponseBytes=1048576
Agent__InitialConversationStatusId=<GUID-del-catalogo>
Agent__ClientParticipantTypeId=<GUID-del-catalogo>
```

Los dos GUID deben corresponder a los catálogos creados por los seeds. Para `dotnet run` en el host use la IP literal `127.0.0.1`, no `localhost`: en Windows "localhost" puede resolver primero a `::1` (IPv6) y el chatbot solo escucha en `127.0.0.1` (IPv4), lo que produce fallos de conexión intermitentes con el agente. Cuando API y agente comparten una red Docker, use `http://chatbot:8010` en `Agent__BaseUrl` (nombre DNS del servicio).

### Telegram

Al habilitar Telegram se inicia un worker que procesa el inbox persistido en Oracle. Además de Oracle, JWT, SMTP y el agente habilitado, configure:

```dotenv
Telegram__Enabled=true
Telegram__GuestModeEnabled=true
Telegram__BotToken=<token-de-BotFather>
Telegram__BotUsername=<nombre-sin-arroba>
Telegram__WebhookSecret=<solo-letras-numeros-guion-y-guion-bajo>
Telegram__PublicWebhookUrl=https://<host-publico>
Telegram__WorkerPollMilliseconds=30000
Telegram__WorkerConcurrency=16
Telegram__ProcessingLeaseSeconds=300
Telegram__MaxProcessingAttempts=3
Telegram__DelegatedTokenMinutes=5
Telegram__OtpPepperBase64=<Base64-de-al-menos-32-bytes-aleatorios>
Telegram__PrivateAccessAbsoluteTtlHours=24
Telegram__PrivateAccessIdleTtlMinutes=30
```

`Telegram__PublicWebhookUrl` debe ser HTTPS. El OTP se guarda como hash con `Telegram__OtpPepperBase64`. Los flujos de vinculación por código (`link-codes`) y de registro web (`telegram/registration/complete`) se retiraron: el alta y la vinculación del cliente van por `POST /api/owners/bot` y `POST /api/integrations/telegram/bot-link`. La guía completa de BotFather, Cloudflare Tunnel, webhook, comandos y diagnóstico está en [docs/integrations/telegram.md](docs/integrations/telegram.md).

## Base de datos y datos iniciales

Restaure las herramientas y dependencias:

```powershell
dotnet tool restore
dotnet restore veterinarian_backend.slnx
```

Con Oracle configurado, aplique las migraciones:

```powershell
dotnet ef database update `
  --project .\src\Infrastructure\Infrastructure.csproj `
  --startup-project .\src\Api\Api.csproj `
  --context VeterinaryDbContext
```

Después, ejecute los seeds idempotentes. Sustituya la ruta y el alias Oracle por los de su entorno; SQL*Plus solicitará la contraseña de la base de datos.

```powershell
$env:NLS_LANG = "SPANISH_SPAIN.AL32UTF8"
& 'C:\ruta\a\sqlplus.exe' `
  'VET_APP@//localhost:1522/FREEPDB1' `
  '@database\seeds\apply_all.sql'
```

Los seeds crean catálogos, roles, permisos y datos de soporte del chat. No incluyen usuarios, contraseñas, dueños, mascotas, citas ni historias clínicas. No ejecute `database/seeds/cleanup_seeds.sql` como parte de una instalación normal. Más detalle en [database/seeds/README.md](database/seeds/README.md).

### Seeds extra y patches

El directorio `database/seeds/extra/` contiene seeds adicionales y parches para casos específicos:

- `apply_all.sql` - Ejecutor principal de seeds de producción (incluye roles, módulos, permisos, catálogos de chat, veterinarios y diagnósticos).
- `diagnostics_seed.sql` - Catálogo inicial de diagnósticos clínicos veterinarios (12 diagnósticos base como Gastroenteritis, Dermatitis, Otitis, etc.).
- `expand_species_races_catalog.sql` - Amplía el catálogo de especies y razas reales (Perro, Gato, Ave, Conejo con múltiples razas).
- `modules_seed.sql` - Catálogo de 23 módulos del sistema incluyendo Chat, Escalamientos, IA y Agente, Catálogos del Chat, Plataforma y Reprogramación de Citas.
- `role_permissions_repair_2026-09-10.sql` - Repara permisos desactualizados en bases existentes, especialmente para roles de staff.
- `status_appointments_seed.sql` - Catálogo canónico de estados de cita (AGENDADA, ATENDIDA, CONFIRMADA, EN_PROGRESO, CANCELADA, NO_ASISTIO).

El directorio `database/patches/` contiene parches para migraciones específicas:

- `add_pet_photo_url.sql` - Agrega columna PHOTO_URL a la tabla PETS para almacenar URLs de fotos de mascotas.
- `grant_auxiliar_citas_create_permission.sql` - Otorga permiso de creación en el módulo Citas al rol Auxiliar.

El seed crea el rol protegido `SuperAdmin`, pero no crea una persona ni una contraseña. Promueva una cuenta interna ya existente siguiendo [docs/SUPERADMIN_PROVISIONING.md](docs/SUPERADMIN_PROVISIONING.md).

## Ejecutar la API

```powershell
dotnet run --project src/Api/Api.csproj --launch-profile http
```

El perfil `http` escucha en `http://localhost:5233`; el perfil `https` usa `https://localhost:7107` y también expone HTTP en el puerto 5233. En `Development`, Swagger queda disponible en `/swagger`. No se expone Swagger automáticamente fuera de Development.

La redirección HTTPS solo se activa cuando se configura una URL o puerto HTTPS, lo que permite trabajar con el perfil HTTP local.

## Superficie HTTP

El inventario completo de métodos y rutas está en [docs/API.md](docs/API.md).
Los DTO, parámetros, respuestas y códigos HTTP vigentes se consultan en
Swagger. A alto nivel:

| Área | Rutas representativas |
| --- | --- |
| Sesión interna | `POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/auth/me`, `GET /api/auth/permissions`, `PATCH /api/auth/me/password`, `POST /api/auth/revoke` |
| Operación de staff | `/api/clients`, `/api/pets`, `/api/veterinarians`, `/api/availabilities`, `/api/veterinarian-absences`, `/api/appointments`, `/api/medicalrecords`, `/api/vaccinations`, `/api/reports` |
| Catálogos y seguridad | `/api/species`, `/api/races`, `/api/services`, `/api/roles`, `/api/role-permissions`, `/api/users`, `PATCH /api/users/{id}/password` |
| Dueños/bot | `POST /api/owners/bot` → `{ clientId }`, lookups anónimos de clientes, Claim OTP (`request-claim-by-identification` + `bot-link/claim`), rutas `/api/bot/*` |
| Integraciones | `POST /api/integrations/telegram/webhook`, `POST /api/integrations/telegram/bot-link`, `POST /api/agent/messages`, `/hubs/notifications` |
| Administración conversacional | `/api/chat/*` (participantes con `clientId`; sin `/api/chat/user-profiles`) |

La política de respaldo exige autenticación para cualquier endpoint que no sea marcado explícitamente como anónimo. Las rutas públicas tienen rate limiting. Los códigos de error estables se devuelven en `application/problem+json`; los clientes deben interpretar el campo `code`, no el texto del mensaje.

## Pruebas

```powershell
dotnet test veterinarian_backend.slnx
```

La solución incluye pruebas unitarias y de integración en `tests/Application.Tests`, `tests/Infrastructure.Tests` y `tests/Api.Tests`.

## Documentación relacionada

- [Integración con Telegram](docs/integrations/telegram.md)
- [Seeds de producción](database/seeds/README.md)
- [Aprovisionamiento de SuperAdmin](docs/SUPERADMIN_PROVISIONING.md)
- [Contexto funcional y de revisión](docs/CONTEXT_REVISION_BACKEND.md)
- [Política de PII en logs](docs/security/pii-logging-policy.md)
- [Documentación de la API](docs/API.md)
- [Catálogo de códigos de error de la API](docs/contracts/api-error-codes-catalog.md)

### ADRs (Architecture Decision Records)

El proyecto mantiene registros de decisiones arquitectónicas en `docs/adr/`:

- [Modelo de Cliente e independencia de OTP](docs/adr/2026-09-04-client-identity-and-otp-boundaries.md) - Define el modelo de Cliente sin contraseña y los límites entre OTP de contacto/identidad y OTP de acción de cita.
- [Límites de permisos del rol Cliente](docs/adr/2026-09-07-client-role-permissions-boundaries.md) - Define la desasignación de permisos de módulos de plataforma web para el rol Cliente.
- [Verificación de contacto por correo](docs/adr/2026-09-07-contact-verification-email-foundations.md) - Establece los fundamentos para la verificación de contacto por correo (propósitos Register y Claim).
- [Retiro del portal Cliente JWT](docs/adr/2026-09-07-etapa-5-client-portal-retirement.md) - Política de retiro de rutas del portal Cliente JWT y convención HTTP 410 Gone.
- [Rate limiting y códigos de error](docs/adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md) - Fundamentos para rate limiting y códigos de error estables.
- [Registro de dueños](docs/adr/2026-09-07-register-owner-foundations.md) - Fundamentos para el registro de dueños sin contraseña.
