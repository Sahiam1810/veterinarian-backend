# Huellitas — Veterinarian Backend

API de Huellitas para la operación de una clínica veterinaria. Centraliza la gestión de personal, dueños, mascotas, agenda, atención clínica, catálogos, notificaciones y la integración con el chatbot.

Está construida con ASP.NET Core 10, Oracle y EF Core, y está organizada en Domain, Application, Infrastructure y Api.

> El portal web es solo para el personal de la clínica. Los dueños no tienen contraseña, cuenta de plataforma ni inicio de sesión web: usan el chatbot de Telegram y verifican su identidad mediante cédula y OTP enviado por correo cuando la operación requiere datos privados.

## Capacidades

- Gestión de personal, roles, permisos por módulo y cuentas internas.
- Dueños, mascotas, especies, razas, especialidades, servicios y diagnósticos.
- Agenda, disponibilidad, ausencias, prevención de solapamientos, citas, historia clínica, vacunas y recordatorios.
- Estados de cuenta y notificaciones en tiempo real mediante SignalR (`/hubs/notifications`).
- JWT RS256 con access token, refresh token rotativo y permisos como claims.
- Integración opcional con el servicio de agente conversacional, SMTP, Twilio y Telegram.
- Verificación de correo por OTP para el alta de dueños desde bot y acciones que necesiten comprobar contacto.
- Administración del runtime conversacional: conversaciones, mensajes, adjuntos, escalaciones, participantes, agentes humanos, modelos y métricas de IA.
- Swagger en Development, rate limiting, CORS, logging estructurado con Serilog y respuestas de error `application/problem+json`.

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

### Dueños: sin contraseña

El rol `Cliente` no puede obtener ni renovar un JWT de plataforma, incluso si existen datos de credenciales heredados. Por tanto, no se debe construir un flujo web de registro o login para dueños.

El flujo vigente es:

1. En Telegram, las consultas veterinarias generales funcionan en modo invitado, si `Telegram__GuestModeEnabled=true`.
2. Cuando una solicitud necesita datos u operaciones privadas, el backend pide cédula y verifica un OTP enviado al correo registrado.
3. Si el dueño aún no existe, confirma sus datos y correo; tras validar el OTP se crea su perfil **sin contraseña** y se vincula el chat.
4. El enlace de Telegram queda persistido. El acceso privado tiene vencimiento absoluto y por inactividad, por lo que un nuevo OTP solo se solicita cuando corresponde.

Las rutas antiguas del portal JWT de cliente (`/api/clients/me`, `/api/pets/mine` y `/api/appointments/mine`) están retiradas y devuelven `410 Gone`. El bot usa las rutas `/api/bot/pets` y `/api/bot/appointments`, protegidas con un JWT delegado interno (`TelegramAgent`) que la API genera; no son endpoints para un navegador ni para llamar con un token de cliente.

## Requisitos

- .NET SDK 10.
- Una instancia Oracle accesible. El proveedor se configura con compatibilidad Oracle 21c.
- Acceso a `sqlplus` si se aplicarán los seeds provistos.
- Un par RSA de al menos 2048 bits, en PEM y codificado en Base64, para JWT.
- Opcional: bot de Telegram, SMTP y el repositorio/servicio del agente conversacional.

La versión de `dotnet-ef` está fijada en `dotnet-tools.json`.

## Configuración local

La API carga un archivo `.env` buscando desde el directorio de ejecución hacia arriba y, después, incorpora las variables de entorno del proceso. Los valores del proceso prevalecen. El archivo `.env` está ignorado por Git y no hay una plantilla `.env.example` versionada: cree su archivo local sin compartir secretos.

Como mínimo, la aplicación exige Oracle, CORS y JWT. Un ejemplo de estructura es el siguiente; reemplace todos los marcadores por valores reales.

```dotenv
ConnectionStrings__DefaultConnection=User Id=VET_APP;Password=<secret>;Data Source=//localhost:1521/FREEPDB1

Cors__AllowedOrigins__0=http://localhost:5173

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

### Opciones de negocio disponibles

`src/Api/appsettings.json` aporta los valores no secretos por defecto:

| Sección | Valores relevantes por defecto |
| --- | --- |
| `AppointmentBooking` | Zona `America/Bogota`, 60 minutos mínimos de anticipación y 30 días máximos. |
| `ContactVerification` | OTP de correo: 10 minutos, 5 intentos, reenvío a los 60 s y proof válido 15 minutos. |
| `RegisterOwner` | `RequireContactProofs=false` para alta hecha por staff; el alta desde bot siempre exige proof. |
| `RateLimiting` | Límite global y límites específicos para login, refresh, Telegram, lookups y OTP. |
| `Reminders` | Worker de recordatorios activo por defecto; ventana y frecuencia configurables. |

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

`ContactVerification__OtpPepperBase64` es opcional si ya se suministra un pepper en `AppointmentVerification__OtpPepperBase64` o en Telegram. Cuando se habilita Telegram, su pepper es obligatorio. Twilio es opcional y requiere `Twilio__AccountSid`, `Twilio__AuthToken` y `Twilio__FromNumber` además de `Twilio__Enabled=true`.

### Servicio del agente

La integración está deshabilitada por defecto. Para activarla se requieren los siguientes valores:

```dotenv
Agent__Enabled=true
Agent__BaseUrl=http://localhost:8000
Agent__MessagesPath=/api/v1/messages
Agent__RequestTimeoutSeconds=30
Agent__MaxResponseBytes=1048576
Agent__InitialConversationStatusId=<GUID-del-catalogo>
Agent__ClientParticipantTypeId=<GUID-del-catalogo>
```

Los dos GUID deben corresponder a los catálogos creados por los seeds. Cuando API y agente comparten una red Docker, use el nombre DNS del servicio en `Agent__BaseUrl`, no `localhost`.

### Telegram

Al habilitar Telegram se inicia un worker que procesa el inbox persistido en Oracle. Además de Oracle, JWT, SMTP y el agente habilitado, configure:

```dotenv
Telegram__Enabled=true
Telegram__GuestModeEnabled=true
Telegram__BotToken=<token-de-BotFather>
Telegram__BotUsername=<nombre-sin-arroba>
Telegram__WebhookSecret=<solo-letras-numeros-guion-y-guion-bajo>
Telegram__PublicWebhookUrl=https://<host-publico>
Telegram__LinkCodeTtlMinutes=10
Telegram__WorkerPollMilliseconds=30000
Telegram__ProcessingLeaseSeconds=300
Telegram__MaxProcessingAttempts=3
Telegram__DelegatedTokenMinutes=5
Telegram__OtpTtlMinutes=5
Telegram__OtpMaximumAttempts=5
Telegram__OtpResendSeconds=60
Telegram__OtpPepperBase64=<Base64-de-al-menos-32-bytes-aleatorios>
Telegram__PrivateAccessAbsoluteTtlHours=24
Telegram__PrivateAccessIdleTtlMinutes=30
Telegram__RegistrationProtectionKeyBase64=<Base64-de-exactamente-32-bytes-aleatorios>
```

`Telegram__PublicWebhookUrl` debe ser HTTPS. La clave de protección cifra los datos temporales del flujo de registro; el OTP se guarda como hash. Si se usa el flujo web de finalización de registro, agregue también `Telegram__RegistrationEnabled=true` y una `Telegram__RegistrationCompletionUrl` HTTPS. La guía completa de BotFather, Cloudflare Tunnel, webhook, comandos y diagnóstico está en [docs/integrations/telegram.md](docs/integrations/telegram.md).

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
  'VET_APP@//localhost:1521/FREEPDB1' `
  '@database\seeds\apply_all.sql'
```

Los seeds crean catálogos, roles, permisos y datos de soporte del chat. No incluyen usuarios, contraseñas, dueños, mascotas, citas ni historias clínicas. No ejecute `database/seeds/cleanup_seeds.sql` como parte de una instalación normal. Más detalle en [database/seeds/README.md](database/seeds/README.md).

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
| Catálogos y seguridad | `/api/species`, `/api/races`, `/api/services`, `/api/roles`, `/api/role-permissions`, `/api/user-permissions`, `/api/users`, `/api/useraccounts`, `/api/usercredentials` |
| Dueños/bot | `POST /api/contact-verification/email/request`, `POST /api/contact-verification/email/confirm`, `POST /api/owners/bot`, lookups anónimos de clientes y rutas internas `/api/bot/*` |
| Integraciones | `POST /api/integrations/telegram/webhook`, `POST /api/integrations/telegram/link-codes`, `POST /api/agent/messages`, `/hubs/notifications` |
| Administración conversacional | `/api/chat/*`, `/api/ai/models`, `/api/ai/providers` |

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
