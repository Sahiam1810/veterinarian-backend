# Huellitas — Veterinarian Backend

Backend del **Sistema de Gestión para Veterinarias "Huellitas"**: la API central que
sostiene la operación de la clínica (usuarios, roles, dueños, mascotas, agenda,
citas, historia clínica) y sirve de puerta de entrada única, tanto para el
frontend web en React como para el agente conversacional en Python
(LangChain/LangGraph + RAG) que atiende por Telegram. Ambos canales
comparten la misma base de datos y el mismo calendario a través de esta API.

Proyecto desarrollado por un equipo de 8 personas dividido en frentes de
backend, frontend, agente/RAG y despliegue.

## Arquitectura

El backend sigue Clean Architecture en cuatro proyectos:

| Proyecto | Responsabilidad |
|---|---|
| `src/Domain` | Entidades y reglas de negocio puras, sin dependencias externas. |
| `src/Application` | Casos de uso (CQRS con MediatR), validaciones (FluentValidation) y contratos de repositorios. |
| `src/Infrastructure` | Implementación de persistencia con EF Core + Oracle, seguridad (JWT), configuración de entidades y migraciones. |
| `src/Api` | Controladores REST, Swagger, autenticación/autorización, CORS, rate limiting y el gateway hacia el agente conversacional. |

Flujo general del sistema completo:

```
Frontend React ──┐
                  ├──► API .NET (este repo) ──► Oracle Database
Agente Python ────┘         │
                             └──► Huellitas ChatBot (Python) ──► Base vectorial (RAG)
```

El agente en Python **no accede directamente a la base de datos**: consulta y
registra todo a través de esta API, exactamente igual que el personal de la
clínica desde el frontend. Esto garantiza que no haya cruces de horario entre
citas creadas manualmente y citas creadas por el agente.

## Características principales

- Autenticación basada en tokens **JWT firmados con RS256** (par de llaves
  pública/privada), con **AccessToken** y **RefreshToken** rotable.
- Endpoints de registro, login, renovación de sesión, revocación de tokens y
  actualización de perfil de usuario.
- **Roles y permisos configurables** desde base de datos (no quemados en
  código): Administrador, Veterinario, Recepcionista, Auxiliar y Cliente,
  combinados en políticas de autorización por endpoint.
- Gestión completa de dueños, mascotas, especies, razas, veterinarios,
  especialidades y disponibilidad horaria.
- Catálogo de servicios, tipos de servicio, diagnósticos y vacunas.
- Calendario y agenda de citas con validación de solapamiento por
  profesional, historial de estados y notificaciones.
- Historia clínica de la mascota y control de vacunación.
- Portal de autoservicio para el cliente (`/me`, `/mine`) para consultar sus
  propias mascotas y citas.
- Gateway hacia el agente conversacional (Huellitas ChatBot): el backend
  deriva la identidad del usuario desde el JWT y reenvía el mensaje, sin
  exponer la base de datos al agente.
- Módulo de administración del chatbot: conversaciones, mensajes, adjuntos,
  participantes, asignaciones a agentes humanos, modelos y proveedores de IA.
- Conexión con la base de datos principal de la compañía (**Oracle**).
- Rate limiting sobre los endpoints sensibles de autenticación
  (`register`, `login`, `refresh`).
- Documentación de cada endpoint en **Swagger/OpenAPI**.
- Manejo de errores centralizado con respuestas de error consistentes
  (`ProblemDetails` + violaciones de validación por campo).
- Logging estructurado con **Serilog**.

## Tecnologías utilizadas

| Componente | Tecnología |
|---|---|
| Lenguaje / Framework | C# · .NET 10 (ASP.NET Core Web API) |
| Base de datos | Oracle Database (EF Core + proveedor Oracle) |
| Autenticación | JWT RS256 (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Patrón de aplicación | CQRS con MediatR, validaciones con FluentValidation |
| Documentación de API | Swashbuckle (Swagger / OpenAPI) |
| Logging | Serilog (consola) |
| Testing | xUnit, `Microsoft.AspNetCore.Mvc.Testing` |
| Configuración de secretos | `DotNetEnv` (archivo `.env`, fuera del repositorio) |

## Prerrequisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/) o superior.
- Acceso a una instancia de **Oracle Database** (local, en contenedor o
  remota) con la cadena de conexión correspondiente.
- Herramienta `dotnet-ef` (versión fijada en `dotnet-tools.json`); se instala
  con `dotnet tool restore`.
- Un par de llaves RSA (PKCS#8 privada / SubjectPublicKeyInfo pública),
  codificadas en Base64, para firmar y validar los JWT.
- (Opcional, para el agente conversacional) el repositorio `Huellitas_ChatBot`
  corriendo junto con Qdrant vía `docker compose`.

> **Despliegue:** el sistema completo (backend, frontend y agente) se
> despliega en un **VPS**, expuesto por dominio y subdominios, idealmente
> mediante contenedores Docker. El VPS y el dominio los provee el
> coordinador del programa.

## Instalación y configuración

1. Clone el repositorio y restaure las herramientas y dependencias:

   ```powershell
   dotnet tool restore
   dotnet restore
   ```

2. Copie `.env.example` como `.env`:

   ```powershell
   Copy-Item .env.example .env
   ```

3. Complete en `.env`:
   - `ConnectionStrings__DefaultConnection`: cadena de conexión a Oracle.
   - `Jwt__PrivateKeyPemBase64` / `Jwt__PublicKeyPemBase64`: par de llaves RSA
     en Base64, más `Jwt__KeyId`, `Jwt__Issuer`, `Jwt__Audience`,
     `Jwt__AccessTokenMinutes`, `Jwt__RefreshTokenDays`, `Jwt__ClockSkewSeconds`.
   - `Cors__AllowedOrigins__0`: origen del frontend React local (por defecto
     `http://localhost:5173` con Vite).
   - Variables `Agent__*` si va a probar la integración con el agente
     conversacional (ver sección [Gateway del agente conversacional](#gateway-del-agente-conversacional)).
   - **Los secretos viven únicamente en `.env`; ese archivo no debe subirse al
     repositorio.**

4. Aplique las migraciones de Entity Framework Core sobre Oracle:

   ```powershell
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   ```

5. (Opcional) Cargue el catálogo inicial de roles ejecutando
   `database/seeds/roles_seed.sql` directamente contra Oracle (SQL*Plus o SQL
   Developer). Los roles no quedan fijos en código: el administrador puede
   crear, editar o eliminar roles adicionales desde la propia aplicación una
   vez desplegada.

## Ejecución

```powershell
dotnet run --project src/Api/Api.csproj --launch-profile http
```

En ambiente `Development`, Swagger está disponible en la URL mostrada por
ASP.NET Core, agregando `/swagger`.

### Pruebas

```powershell
dotnet test
```

El proyecto incluye pruebas unitarias y de integración en `tests/Api.Tests`,
`tests/Application.Tests` y `tests/Infrastructure.Tests` (xUnit).

## Roles y políticas de autorización

Los roles se administran desde base de datos (tabla `ROLES`, módulo
`/api/roles`) y viajan en el claim `role` del JWT. La API expone políticas de
autorización que combinan uno o más roles por endpoint:

| Política | Rol(es) requerido(s) |
|---|---|
| `AdminOnly` | Administrador |
| `VeterinarianOnly` | Veterinario |
| `ReceptionistOnly` | Recepcionista |
| `AssistantOnly` | Auxiliar |
| `ClientOnly` | Cliente |
| `StaffOnly` | Administrador, Veterinario, Recepcionista, Auxiliar |
| `AdminOrReceptionist` | Administrador, Recepcionista |
| `AdminOrVeterinarian` | Administrador, Veterinario |
| `ClinicalStaffOnly` | Administrador, Veterinario, Recepcionista |
| `FrontDeskStaffOnly` | Administrador, Recepcionista, Auxiliar |
| `ClinicalHistoryReadOnly` | Administrador, Veterinario, Recepcionista, Cliente (lectura de historia clínica y vacunas) |

Todo endpoint que no declare explícitamente `[Authorize]` ni
`[AllowAnonymous]` exige de todas formas un JWT válido (política de
respaldo). Los endpoints públicos son únicamente `register`, `login` y
`refresh` en `/api/auth`.

## Modelo de datos (resumen de entidades)

La base de datos se diseñó en equipo, con retroalimentación conjunta sobre
cada entidad y relación antes de escribir código. Puede sufrir ajustes
menores a medida que se detecten relaciones o tablas faltantes durante el
desarrollo.

### Negocio general

| Entidad | Descripción |
|---|---|
| `users` | Usuarios base del sistema (veterinario, cliente, cuenta administrativa). |
| `roles` | Roles disponibles para los usuarios. |
| `specialties` | Especialidad de cada veterinario. |
| `veterinarians` | Veterinarios, derivados de `users`. |
| `availabilities` | Disponibilidad horaria de cada veterinario. |
| `account_statements` | Estados de cuenta. |
| `user_accounts` | Cuentas por usuario. |
| `user_credentials` | Credenciales (usuario/contraseña) de la cuenta. |
| `user_tokens` | Tokens de sesión (JWT / refresh). |
| `type_services` | Tipos de servicio. |
| `services` | Servicios ofrecidos por la clínica. |
| `diagnostics` | Catálogo de diagnósticos/enfermedades. |
| `notifications` | Notificaciones para el usuario, generadas a partir de citas. |
| `race` | Razas de mascotas. |
| `species` | Especies de mascotas. |
| `pets` | Mascotas, con su raza y especie. |
| `clients` | Clientes (dueños) asociados a un usuario. |
| `clients_pets` | Relación entre clientes y sus mascotas. |
| `status_appointments` | Estados posibles de una cita. |
| `appointments` | Información general de la cita. |
| `appointment_status_histories` | Historial de estados por los que pasó una cita. |
| `medical_records` | Historia clínica de una mascota atendida. |
| `vaccinations` | Catálogo y registro de vacunas aplicadas. |

### Chatbot

| Entidad | Descripción |
|---|---|
| `provider_models_ai`, `ai_models`, `ai_runs_statuses` | Proveedores y modelos de IA usados por el agente, y estados de sus ejecuciones. |
| `chat_ai_runs`, `chat_ai_run_metrics`, `chat_ai_run_errors` | Ejecuciones del agente de IA, sus métricas y errores. |
| `chat_conversation_ai_settings` | Configuración de IA por conversación (habilitada/deshabilitada, modelo por defecto). |
| `conversations_statuses`, `priority` | Estados y prioridad de una conversación. |
| `chat_conversations` | Conversaciones del chat. |
| `sender_types`, `chat_participants`, `chat_user_profiles`, `agent_humans` | Tipos de remitente, participantes de una conversación, perfiles de chat y agentes humanos. |
| `chat_messages`, `message_types` | Mensajes del chat y su tipo. |
| `chat_attachments` | Adjuntos de un mensaje. |
| `chat_conversation_assignments` | Asignación de una conversación a un agente humano. |
| `chat_escalations`, `chat_escalation_resolution`, `chat_escalation_assignments`, `chat_escalation_status_history`, `escalations_statuses` | Escalamiento de una conversación a un agente humano y su resolución. |

## Listado de endpoints

Todas las rutas están prefijadas con `api/`. Salvo que se indique
"Anónimo", cada endpoint exige un JWT válido y, cuando aplica, la política de
autorización indicada.

### Autenticación, usuarios y cuentas

| Método | Ruta | Autorización | Descripción |
|---|---|---|---|
| POST | `/api/auth/register` | Anónimo (rate limit) | Registra un nuevo usuario y emite AccessToken/RefreshToken iniciales. |
| POST | `/api/auth/login` | Anónimo (rate limit) | Valida credenciales y genera tokens de acceso. |
| POST | `/api/auth/refresh` | Anónimo (rate limit) | Renueva los tokens usando un RefreshToken válido. |
| GET | `/api/auth/me` | Autenticado | Retorna el perfil del usuario autenticado. |
| POST | `/api/auth/revoke` | Autenticado | Revoca un RefreshToken y cierra la sesión. |
| POST | `/api/users` | AdminOnly | Crea un nuevo usuario y le asigna un rol. |
| GET | `/api/users` | AdminOnly | Lista todos los usuarios. |
| GET | `/api/users/{id}` | AdminOnly | Obtiene un usuario por ID. |
| PUT | `/api/users/{id}` | AdminOnly | Actualiza nombre, correo o rol de un usuario. |
| PATCH | `/api/users/{id}/deactivate` | AdminOnly | Desactiva un usuario. |
| PATCH | `/api/users/{id}/activate` | AdminOnly | Reactiva un usuario previamente desactivado. |
| POST | `/api/useraccounts` | AdminOnly | Crea la cuenta de acceso de un usuario existente. |
| GET | `/api/useraccounts` | AdminOnly | Lista todas las cuentas de usuario. |
| GET | `/api/useraccounts/{id}` | AdminOnly | Obtiene una cuenta por ID. |
| PUT | `/api/useraccounts/{id}` | AdminOnly | Actualiza usuario, correo o estado de una cuenta. |
| DELETE | `/api/useraccounts/{id}` | AdminOnly | Elimina una cuenta de usuario. |
| POST | `/api/usercredentials` | AdminOnly | Registra la contraseña inicial (hash) de una cuenta. |
| GET | `/api/usercredentials/{id}` | AdminOnly | Obtiene metadatos de credenciales por ID. |
| GET | `/api/usercredentials/by-account/{accountId}` | AdminOnly | Obtiene metadatos de credenciales de una cuenta. |
| PATCH | `/api/usercredentials/{id}/change-password` | AdminOnly | Valida y cambia la contraseña de una cuenta. |
| POST | `/api/usertokens` | AdminOnly | Registra un nuevo token de sesión. |
| GET | `/api/usertokens/{id}` | AdminOnly | Obtiene un token por ID. |
| GET | `/api/usertokens/by-account/{accountId}` | AdminOnly | Lista los tokens de una cuenta. |
| DELETE | `/api/usertokens/{id}` | AdminOnly | Revoca/elimina un token. |
| POST | `/api/roles` | AdminOnly | Crea un nuevo rol. |
| GET | `/api/roles` | AdminOnly | Lista todos los roles. |
| GET | `/api/roles/{id}` | AdminOnly | Obtiene un rol por ID. |
| PUT | `/api/roles/{id}` | AdminOnly | Actualiza nombre/descripción de un rol. |
| DELETE | `/api/roles/{id}` | AdminOnly | Elimina un rol. |

### Dueños, mascotas y profesionales

| Método | Ruta | Autorización | Descripción |
|---|---|---|---|
| GET | `/api/clients/me` | ClientOnly | Perfil del cliente autenticado (portal del dueño). |
| GET | `/api/clients` | StaffOnly | Lista todos los clientes. |
| GET | `/api/clients/{id}` | StaffOnly | Obtiene un cliente por ID. |
| POST | `/api/clients` | FrontDeskStaffOnly | Registra un cliente asociado a un usuario existente. |
| PUT | `/api/clients/{id}` | FrontDeskStaffOnly | Actualiza los datos de un cliente. |
| DELETE | `/api/clients/{id}` | AdminOnly | Elimina un cliente. |
| GET | `/api/pets/mine` | ClientOnly | Mascotas del cliente autenticado (portal del dueño). |
| GET | `/api/pets` | StaffOnly | Lista todas las mascotas. |
| GET | `/api/pets/{id}` | StaffOnly | Obtiene una mascota por ID. |
| POST | `/api/pets` | FrontDeskStaffOnly | Registra una mascota con especie y raza. |
| PUT | `/api/pets/{id}` | FrontDeskStaffOnly | Actualiza los datos de una mascota. |
| DELETE | `/api/pets/{id}` | AdminOnly | Elimina una mascota. |
| GET | `/api/clientspets` | StaffOnly | Lista las relaciones cliente-mascota. |
| GET | `/api/clientspets/{id}` | StaffOnly | Obtiene una relación cliente-mascota por ID. |
| POST | `/api/clientspets` | FrontDeskStaffOnly | Crea una asociación cliente-mascota. |
| PUT | `/api/clientspets/{id}` | FrontDeskStaffOnly | Actualiza si el cliente es dueño principal. |
| DELETE | `/api/clientspets/{id}` | AdminOnly | Elimina una asociación cliente-mascota. |
| GET | `/api/species` | StaffOnly | Lista las especies. |
| GET | `/api/species/{id}` | StaffOnly | Obtiene una especie por ID. |
| POST | `/api/species` | AdminOnly | Crea una especie. |
| PUT | `/api/species/{id}` | AdminOnly | Actualiza una especie. |
| DELETE | `/api/species/{id}` | AdminOnly | Elimina una especie. |
| GET | `/api/races` | StaffOnly | Lista las razas. |
| GET | `/api/races/{id}` | StaffOnly | Obtiene una raza por ID. |
| POST | `/api/races` | AdminOnly | Crea una raza. |
| PUT | `/api/races/{id}` | AdminOnly | Actualiza una raza. |
| DELETE | `/api/races/{id}` | AdminOnly | Elimina una raza. |
| POST | `/api/veterinarians` | AdminOnly | Registra un veterinario (usuario, especialidad, matrícula). |
| GET | `/api/veterinarians` | StaffOnly | Lista todos los veterinarios. |
| GET | `/api/veterinarians/{id}` | StaffOnly | Obtiene un veterinario por ID. |
| PUT | `/api/veterinarians/{id}` | AdminOnly | Actualiza un veterinario. |
| DELETE | `/api/veterinarians/{id}` | AdminOnly | Elimina un veterinario. |
| GET | `/api/specialties` | StaffOnly | Lista las especialidades. |
| GET | `/api/specialties/{id}` | StaffOnly | Obtiene una especialidad por ID. |
| POST | `/api/specialties` | AdminOnly | Crea una especialidad. |
| PUT | `/api/specialties/{id}` | AdminOnly | Actualiza una especialidad. |
| DELETE | `/api/specialties/{id}` | AdminOnly | Elimina una especialidad. |
| POST | `/api/availabilities` | AdminOrReceptionist | Crea un bloque de disponibilidad semanal de un veterinario. |
| GET | `/api/availabilities` | StaffOnly | Lista todos los bloques de disponibilidad. |
| GET | `/api/availabilities/{id}` | StaffOnly | Obtiene un bloque de disponibilidad por ID. |
| GET | `/api/availabilities/by-veterinarian/{veterinarianId}` | StaffOnly | Lista la disponibilidad de un veterinario. |
| PUT | `/api/availabilities/{id}` | AdminOrReceptionist | Actualiza día/hora/estado de un bloque. |
| DELETE | `/api/availabilities/{id}` | AdminOrReceptionist | Elimina un bloque de disponibilidad. |

### Servicios y catálogos

| Método | Ruta | Autorización | Descripción |
|---|---|---|---|
| POST | `/api/services` | AdminOnly | Crea un servicio veterinario. |
| GET | `/api/services` | StaffOnly | Lista todos los servicios. |
| GET | `/api/services/{id}` | StaffOnly | Obtiene un servicio por ID. |
| PUT | `/api/services/{id}` | AdminOnly | Actualiza un servicio. |
| DELETE | `/api/services/{id}` | AdminOnly | Elimina un servicio. |
| POST | `/api/typeservices` | AdminOnly | Crea un tipo de servicio. |
| GET | `/api/typeservices` | StaffOnly | Lista los tipos de servicio. |
| GET | `/api/typeservices/{id}` | StaffOnly | Obtiene un tipo de servicio por ID. |
| PUT | `/api/typeservices/{id}` | AdminOnly | Actualiza un tipo de servicio. |
| DELETE | `/api/typeservices/{id}` | AdminOnly | Elimina un tipo de servicio. |
| GET | `/api/diagnostics?onlyActive=` | StaffOnly | Lista el catálogo de diagnósticos (activos por defecto). |
| GET | `/api/diagnostics/{id}` | StaffOnly | Obtiene un diagnóstico por ID. |
| POST | `/api/diagnostics` | AdminOrVeterinarian | Crea un diagnóstico clínico. |
| PUT | `/api/diagnostics/{id}` | AdminOrVeterinarian | Actualiza código, nombre, descripción o estado de un diagnóstico. |
| DELETE | `/api/diagnostics/{id}` | AdminOnly | Desactiva (borrado lógico) un diagnóstico. |
| POST | `/api/vaccinations` | AdminOrVeterinarian | Registra una vacuna aplicada a la mascota de un cliente. |
| GET | `/api/vaccinations` | ClinicalHistoryReadOnly | Lista los registros de vacunación. |
| GET | `/api/vaccinations/{id}` | ClinicalHistoryReadOnly | Obtiene un registro de vacunación por ID. |
| PUT | `/api/vaccinations/{id}` | AdminOnly | Corrige un registro de vacunación existente. |
| POST | `/api/statusappointments` | AdminOnly | Crea un estado de cita (p. ej. Pendiente, Confirmada). |
| GET | `/api/statusappointments` | StaffOnly | Lista los estados de cita. |
| GET | `/api/statusappointments/{id}` | StaffOnly | Obtiene un estado de cita por ID. |
| PUT | `/api/statusappointments/{id}` | AdminOnly | Actualiza un estado de cita. |
| DELETE | `/api/statusappointments/{id}` | AdminOnly | Elimina un estado de cita. |
| GET | `/api/airunstatuses` | AdminOnly | Lista los estados de ejecución de IA. |
| GET | `/api/airunstatuses/{id}` | AdminOnly | Obtiene un estado de ejecución de IA por ID. |
| POST | `/api/airunstatuses` | AdminOnly | Crea un estado de ejecución de IA. |
| PUT | `/api/airunstatuses/{id}` | AdminOnly | Actualiza el nombre de un estado de ejecución de IA. |
| DELETE | `/api/airunstatuses/{id}` | AdminOnly | Elimina un estado de ejecución de IA. |
| GET | `/api/conversationstatuses` | AdminOnly | Lista los estados de conversación del chat. |
| GET | `/api/conversationstatuses/{id}` | AdminOnly | Obtiene un estado de conversación por ID. |
| POST | `/api/conversationstatuses` | AdminOnly | Crea un estado de conversación. |
| PUT | `/api/conversationstatuses/{id}` | AdminOnly | Actualiza el nombre de un estado de conversación. |
| DELETE | `/api/conversationstatuses/{id}` | AdminOnly | Elimina un estado de conversación. |
| GET | `/api/escalationstatuses` | AdminOnly | Lista los estados de escalamiento. |
| GET | `/api/escalationstatuses/{id}` | AdminOnly | Obtiene un estado de escalamiento por ID. |
| POST | `/api/escalationstatuses` | AdminOnly | Crea un estado de escalamiento. |
| PUT | `/api/escalationstatuses/{id}` | AdminOnly | Actualiza el nombre de un estado de escalamiento. |
| DELETE | `/api/escalationstatuses/{id}` | AdminOnly | Elimina un estado de escalamiento. |
| GET | `/api/messagetypes` | AdminOnly | Lista los tipos de mensaje del chat. |
| GET | `/api/messagetypes/{id}` | AdminOnly | Obtiene un tipo de mensaje por ID. |
| POST | `/api/messagetypes` | AdminOnly | Crea un tipo de mensaje. |
| PUT | `/api/messagetypes/{id}` | AdminOnly | Actualiza el nombre de un tipo de mensaje. |
| DELETE | `/api/messagetypes/{id}` | AdminOnly | Elimina un tipo de mensaje. |
| GET | `/api/priorities` | StaffOnly | Lista los niveles de prioridad. |
| GET | `/api/priorities/{id}` | StaffOnly | Obtiene un nivel de prioridad por ID. |
| POST | `/api/priorities` | AdminOnly | Crea un nivel de prioridad. |
| PUT | `/api/priorities/{id}` | AdminOnly | Actualiza el nombre de un nivel de prioridad. |
| DELETE | `/api/priorities/{id}` | AdminOnly | Elimina un nivel de prioridad. |
| GET | `/api/sendertypes` | AdminOnly | Lista los tipos de remitente del chat. |
| GET | `/api/sendertypes/{id}` | AdminOnly | Obtiene un tipo de remitente por ID. |
| POST | `/api/sendertypes` | AdminOnly | Crea un tipo de remitente. |
| PUT | `/api/sendertypes/{id}` | AdminOnly | Actualiza el nombre de un tipo de remitente. |
| DELETE | `/api/sendertypes/{id}` | AdminOnly | Elimina un tipo de remitente. |

### Citas e historia clínica

| Método | Ruta | Autorización | Descripción |
|---|---|---|---|
| GET | `/api/appointments/mine` | ClientOnly | Citas propias del cliente autenticado. |
| POST | `/api/appointments` | AdminOrReceptionist | Crea una cita médica. |
| GET | `/api/appointments` | StaffOnly | Lista todas las citas. |
| GET | `/api/appointments/{id}` | StaffOnly | Obtiene una cita por ID. |
| PUT | `/api/appointments/{id}` | AdminOrReceptionist | Actualiza una cita existente. |
| DELETE | `/api/appointments/{id}` | AdminOrReceptionist | Elimina una cita. |
| POST | `/api/appointmentstatushistories` | ClinicalStaffOnly | Registra un cambio de estado de una cita. |
| GET | `/api/appointmentstatushistories` | StaffOnly | Lista el historial de estados de citas. |
| GET | `/api/appointmentstatushistories/{id}` | StaffOnly | Obtiene una entrada del historial por ID. |
| PUT | `/api/appointmentstatushistories/{id}` | ClinicalStaffOnly | Actualiza una entrada del historial. |
| DELETE | `/api/appointmentstatushistories/{id}` | AdminOnly | Elimina una entrada del historial. |
| POST | `/api/medicalrecords` | AdminOrVeterinarian | Crea un registro clínico (inmutable) de una mascota. |
| GET | `/api/medicalrecords` | ClinicalHistoryReadOnly | Lista los registros clínicos. |
| GET | `/api/medicalrecords/{id}` | ClinicalHistoryReadOnly | Obtiene un registro clínico por ID. |

### Notificaciones y estados de cuenta

| Método | Ruta | Autorización | Descripción |
|---|---|---|---|
| POST | `/api/notifications` | StaffOnly | Crea una notificación asociada a un usuario y una cita. |
| GET | `/api/notifications` | StaffOnly | Lista todas las notificaciones. |
| GET | `/api/notifications/{id}` | StaffOnly | Obtiene una notificación por ID. |
| GET | `/api/notifications/user/{userId}` | StaffOnly | Lista las notificaciones de un usuario. |
| GET | `/api/notifications/appointment/{appointmentId}` | StaffOnly | Lista las notificaciones de una cita. |
| PUT | `/api/notifications/{id}` | StaffOnly | Actualiza una notificación. |
| DELETE | `/api/notifications/{id}` | StaffOnly | Elimina una notificación. |
| POST | `/api/accountstatements` | AdminOrReceptionist | Genera un estado de cuenta para una cuenta de usuario. |
| GET | `/api/accountstatements/{id}` | StaffOnly | Obtiene un estado de cuenta por ID. |
| GET | `/api/accountstatements/by-account/{accountId}` | StaffOnly | Lista los estados de cuenta de una cuenta. |
| PATCH | `/api/accountstatements/{id}/status` | AdminOrReceptionist | Cambia el estado de un estado de cuenta (p. ej. pagado). |
| DELETE | `/api/accountstatements/{id}` | AdminOrReceptionist | Elimina un estado de cuenta. |

### Agente conversacional y agentes humanos

| Método | Ruta | Autorización | Descripción |
|---|---|---|---|
| POST | `/api/agent/messages` | Autenticado (identidad tomada del JWT) | Reenvía un mensaje de chat al servicio interno del agente conversacional. Ver detalle abajo. |
| POST | `/api/chat/agent-humans` | AdminOnly | Registra un agente humano para un usuario existente. |
| GET | `/api/chat/agent-humans` | AdminOnly | Lista todos los agentes humanos. |
| GET | `/api/chat/agent-humans/{id}` | AdminOnly | Obtiene un agente humano por ID. |
| GET | `/api/chat/agent-humans/by-user/{userId}` | AdminOnly | Lista los agentes humanos de un usuario. |
| PUT | `/api/chat/agent-humans/{id}` | AdminOnly | Actualiza/verifica un agente humano. |
| PATCH | `/api/chat/agent-humans/{id}/activate` | AdminOnly | Activa un agente humano. |
| PATCH | `/api/chat/agent-humans/{id}/deactivate` | AdminOnly | Desactiva un agente humano. |

### Administración del chatbot y modelos de IA

| Método | Ruta | Autorización | Descripción |
|---|---|---|---|
| POST | `/api/ai/models` | AdminOnly | Crea un modelo de IA bajo un proveedor. |
| GET | `/api/ai/models/{id}` | AdminOnly | Obtiene un modelo de IA por ID. |
| GET | `/api/ai/models` | AdminOnly | Lista todos los modelos de IA. |
| GET | `/api/ai/models/by-provider/{providerId}` | AdminOnly | Lista los modelos de un proveedor. |
| PUT | `/api/ai/models/{id}` | AdminOnly | Actualiza metadatos, precios y límites de tokens de un modelo. |
| PATCH | `/api/ai/models/{id}/activate` | AdminOnly | Activa un modelo de IA. |
| PATCH | `/api/ai/models/{id}/deactivate` | AdminOnly | Desactiva un modelo de IA. |
| POST | `/api/ai/providers` | AdminOnly | Registra un proveedor de IA. |
| GET | `/api/ai/providers/{id}` | AdminOnly | Obtiene un proveedor de IA por ID. |
| GET | `/api/ai/providers` | AdminOnly | Lista todos los proveedores de IA. |
| PUT | `/api/ai/providers/{id}` | AdminOnly | Actualiza nombre, razón social o sitio web de un proveedor. |
| PATCH | `/api/ai/providers/{id}/activate` | AdminOnly | Activa un proveedor de IA. |
| PATCH | `/api/ai/providers/{id}/deactivate` | AdminOnly | Desactiva un proveedor de IA. |
| POST | `/api/chat/user-profiles` | AdminOnly | Crea un perfil de chat para un usuario existente. |
| GET | `/api/chat/user-profiles` | AdminOnly | Lista todos los perfiles de chat. |
| GET | `/api/chat/user-profiles/{id}` | AdminOnly | Obtiene un perfil de chat por ID. |
| GET | `/api/chat/user-profiles/by-user/{userId}` | AdminOnly | Lista los perfiles de chat de un usuario. |
| PUT | `/api/chat/user-profiles/{id}` | AdminOnly | Actualiza nombre visible, avatar y biografía de un perfil. |
| DELETE | `/api/chat/user-profiles/{id}` | AdminOnly | Elimina un perfil de chat. |
| POST | `/api/chat/conversations` | AdminOnly | Crea una conversación de chat (IA habilitada por defecto). |
| GET | `/api/chat/conversations` | AdminOnly | Lista todas las conversaciones. |
| GET | `/api/chat/conversations/{id}` | AdminOnly | Obtiene una conversación por ID. |
| PATCH | `/api/chat/conversations/{id}/status` | AdminOnly | Cambia el estado de una conversación. |
| PATCH | `/api/chat/conversations/{id}/priority` | AdminOnly | Define o limpia la prioridad de una conversación. |
| PATCH | `/api/chat/conversations/{id}/ai-enabled` | AdminOnly | Activa/desactiva el procesamiento por IA de una conversación. |
| PATCH | `/api/chat/conversations/{id}/close` | AdminOnly | Cierra una conversación. |
| PATCH | `/api/chat/conversations/{id}/reopen` | AdminOnly | Reabre una conversación cerrada. |
| POST | `/api/chat/conversation-ai-settings` | AdminOnly | Crea la configuración de IA de una conversación. |
| GET | `/api/chat/conversation-ai-settings` | AdminOnly | Lista todas las configuraciones de IA por conversación. |
| GET | `/api/chat/conversation-ai-settings/{id}` | AdminOnly | Obtiene una configuración de IA por ID. |
| GET | `/api/chat/conversation-ai-settings/by-conversation/{conversationId}` | AdminOnly | Obtiene la última configuración de IA de una conversación. |
| PUT | `/api/chat/conversation-ai-settings/{id}` | AdminOnly | Actualiza si la IA está habilitada y el modelo por defecto. |
| DELETE | `/api/chat/conversation-ai-settings/{id}` | AdminOnly | Elimina una configuración de IA. |
| POST | `/api/chat/conversation-assignments` | AdminOnly | Asigna un agente humano a una conversación. |
| GET | `/api/chat/conversation-assignments` | AdminOnly | Lista todas las asignaciones. |
| GET | `/api/chat/conversation-assignments/{chatConversationId}` | AdminOnly | Obtiene la asignación de una conversación. |
| GET | `/api/chat/conversation-assignments/by-agent/{agentHumanId}` | AdminOnly | Lista las asignaciones de un agente humano. |
| PUT | `/api/chat/conversation-assignments/{chatConversationId}` | AdminOnly | Actualiza el agente o las fechas de una asignación. |
| DELETE | `/api/chat/conversation-assignments/{chatConversationId}` | AdminOnly | Elimina una asignación. |
| POST | `/api/chat/participants` | AdminOnly | Agrega un participante (perfil, agente humano o modelo de IA) a una conversación. |
| GET | `/api/chat/participants/{id}` | AdminOnly | Obtiene un participante por ID. |
| GET | `/api/chat/participants/conversation/{chatConversationId}` | AdminOnly | Lista los participantes de una conversación. |
| PATCH | `/api/chat/participants/{id}/identity` | AdminOnly | Cambia la identidad referenciada por un participante. |
| POST | `/api/chat/attachments` | AdminOnly | Agrega un adjunto a un mensaje. |
| GET | `/api/chat/attachments/{id}` | AdminOnly | Obtiene un adjunto por ID. |
| GET | `/api/chat/attachments/message/{chatMessageId}` | AdminOnly | Lista los adjuntos de un mensaje. |
| POST | `/api/chat/messages` | AdminOnly | Crea un mensaje dentro de una conversación. |
| GET | `/api/chat/messages/{id}` | AdminOnly | Obtiene un mensaje por ID. |
| GET | `/api/chat/messages/conversation/{chatConversationId}` | AdminOnly | Lista los mensajes de una conversación. |

## Gateway del agente conversacional

El módulo `Agent` expone `POST /api/agent/messages`. El cliente llama solamente
al backend .NET; el backend deriva `person_id` y `role` del JWT validado y
reenvía internamente la solicitud a Huellitas ChatBot.

Configure estas variables en `.env`:

```dotenv
Agent__Enabled=true
Agent__BaseUrl=http://localhost:8000
Agent__MessagesPath=/api/v1/messages
Agent__RequestTimeoutSeconds=30
Agent__MaxResponseBytes=1048576
Agent__InitialConversationStatusId=81000000-0000-0000-0000-000000000001
Agent__ClientParticipantTypeId=82000000-0000-0000-0000-000000000001
```

Antes de habilitar el gateway por primera vez, ejecute el seed idempotente
`database/seeds/chat_conversation_catalogs_seed.sql` en el esquema Oracle del
backend. El script registra el estado inicial `Abierta` y el tipo de
participante `Cliente` con los mismos identificadores configurados arriba. No
incluye credenciales y puede ejecutarse nuevamente sin duplicar esos registros.

Cuando backend y chatbot estén en la misma red de Docker, use el nombre DNS del
servicio en lugar de `localhost`, por ejemplo:

```dotenv
Agent__BaseUrl=http://agent-api:8000
```

### Prueba desde Swagger

1. Inicie Huellitas ChatBot y Qdrant con `docker compose` desde el repositorio
   `Huellitas_ChatBot`.
2. Inicie este backend con `Agent__Enabled=true`.
3. Obtenga un access token mediante `POST /api/auth/login` o
   `POST /api/auth/register`.
4. Autorice Swagger con el access token.
5. Ejecute `POST /api/agent/messages`. Los headers `Idempotency-Key` y
   `X-Correlation-ID` son opcionales: si se omiten, el backend genera una clave
   `msg-{UUID}` y un identificador de correlación respectivamente.
6. Para reintentar de forma controlada una misma operación, reutilice el mismo
   `Idempotency-Key`; una clave generada por el backend identifica solamente la
   llamada actual mientras no exista persistencia durable de mensajes.
7. Reutilice el `conversationId` retornado en los mensajes siguientes del mismo
   hilo.

Solicitud inicial:

```json
{
  "message": "¿Qué vacunas necesita mi mascota?",
  "conversationId": null,
  "petId": null,
  "language": "es-CO"
}
```

El contrato público no permite enviar `userId`, `roles`, `channel`,
`isEscalated` ni `publishAsGlobalKnowledge`. Esos valores son controlados por
el backend.

La respuesta incluye los metadatos del agente `provider`, `model`, `usage`,
`module` y `rag`, además del mensaje y los identificadores de conversación y
correlación. Los campos no aplicables pueden retornar `null`.

### Contexto persistente del agente

`Agent__ConversationContextTtlSeconds` y
`Agent__ConversationContextCapacity` fueron retiradas porque configuraban el
proveedor transitorio en memoria. El contexto actual se conserva en Oracle y
utiliza los catálogos indicados por `Agent__InitialConversationStatusId` y
`Agent__ClientParticipantTypeId`.

## Canal Telegram

El backend expone un webhook técnico y un endpoint autenticado para generar
códigos de vinculación. Al habilitar el canal, un worker procesa el inbox de
Oracle, reutiliza el dispatcher del agente y devuelve texto al chat privado.
La primera fase no escribe historial en `CHAT_MESSAGES`.

Las variables requeridas están documentadas en `.env.example`. La guía de
BotFather, túnel HTTPS, `setWebhook` y prueba desde Swagger está en
[`docs/integrations/telegram.md`](docs/integrations/telegram.md).

Un chat sin vincular puede usar `/registrar` para crear una cuenta de cliente.
El bot solicita únicamente el correo y lo verifica mediante OTP. Después envía
un enlace HTTPS de un solo uso donde se diligencian nombre, identificación,
usuario y contraseña; esos datos no pasan por Telegram ni por el agente Python.
Al completar el formulario, el backend crea `Users`, `UserAccounts`,
`UserCredentials`, `Clients` y `TelegramUserLinks` en una transacción.

Para generar la clave local que protege el correo verificado:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Configure el resultado en `Telegram__RegistrationProtectionKeyBase64` y use
como `Telegram__RegistrationCompletionUrl` la URL pública HTTPS del backend
seguida de `/telegram/registration/complete`. Cuando exista el frontend
definitivo, esa URL podrá cambiarse sin mover la lógica de registro de .NET.

Por ahora, los identificadores generados se mantienen en memoria con TTL y
capacidad limitada. No representan historial canónico y se pierden al reiniciar
la API. El futuro módulo especializado de conversaciones reemplazará este
proveedor mediante inyección de dependencias, sin modificar el endpoint ni el
caso de uso de `Agent`.

## Repositorios relacionados

| Repositorio | Rol |
|---|---|
| `veterinarian-backend` (este repo) | API .NET — lógica de negocio, datos y seguridad. |
| `veterinarian-fronted` | Frontend React para el personal de la clínica. |
| `Huellitas_ChatBot` | Agente conversacional en Python (LangChain/LangGraph) con RAG. |

## Equipo y responsables

| Integrante (usuario) | Rol en el proyecto | Contacto |
|---|---|---|
| Sahiam1810 | _Diligenciar_ | esteban.sahiam2017@gmail.com |
| Jhoan2007MA | _Diligenciar_ | _Diligenciar_ |
| Ksanti-monsalve | _Diligenciar_ | _Diligenciar_ |
| Samuek2006 | _Diligenciar_ | _Diligenciar_ |
| Tomfmp2 | _Diligenciar_ | _Diligenciar_ |
| santiagoGal7 | _Diligenciar_ | _Diligenciar_ |
| spostre | _Diligenciar_ | _Diligenciar_ |

> Completar rol (líder, backend, frontend, agente/RAG, despliegue) y
> contacto de cada integrante según el formato de la sección 11 del
> documento de requerimientos (registro de actividades por integrante).
