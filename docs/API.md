# Catálogo de endpoints

Este documento inventaría las rutas implementadas en `src/Api`. Los cuerpos,
parámetros, respuestas y códigos HTTP precisos se publican en Swagger al
ejecutar la API en `Development` (`/swagger`); esa es la fuente de contrato
para una integración.

## Convenciones de acceso

- **Staff/permisos**: JWT de una cuenta interna y el permiso o política que
  declara el controlador. `SuperAdmin` puede realizar las operaciones que le
  correspondan por política.
- **Admin**: política `AdminOnly`; **SuperAdmin**: política `SuperAdminOnly`.
- **Bot interno**: JWT delegado con el claim `TelegramAgent`, emitido por la
  propia API. No es un token para navegador ni para dueños.
- **Anónimo**: no requiere JWT, pero puede requerir encabezado, OTP o proof y
  tiene rate limiting cuando se indica.
- **Legacy (410)**: ruta conservada únicamente para indicar que el portal JWT
  de cliente fue retirado. Siempre responde `410 Gone`.

Las rutas sin una marca explícita en este catálogo requieren un JWT por la
política de respaldo de la API. Las rutas, los nombres de parámetros y los
GUID se muestran tal como están definidos en los atributos de enrutamiento.

## Sesión, contacto y dueños

| Método | Ruta | Acceso | Uso |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | Anónimo, rate limit | Inicia sesión de personal. |
| POST | `/api/auth/refresh` | Anónimo, rate limit | Rota un refresh token de personal. |
| GET | `/api/auth/me` | Autenticado | Perfil de la cuenta interna actual. |
| GET | `/api/auth/permissions` | Autenticado | Permisos efectivos de la cuenta actual. |
| PATCH | `/api/auth/me/password` | Autenticado | Cambia la contraseña propia. |
| POST | `/api/auth/revoke` | Autenticado | Revoca un refresh token propio. |
| POST | `/api/contact-verification/email/request` | Anónimo, rate limit | Solicita OTP de correo. |
| POST | `/api/contact-verification/email/confirm` | Anónimo, rate limit | Confirma OTP y devuelve proof de un solo uso. |
| POST | `/api/owners/bot` | Anónimo, rate limit + proof | Crea un dueño desde el bot, sin contraseña ni cuenta web. |
| GET | `/api/clients/by-identification/{identificationNumber}` | Anónimo, rate limit | Lookup acotado por cédula para el bot. |
| GET | `/api/clients/by-phone/{phone}` | Anónimo, rate limit | Lookup acotado por teléfono para el bot. |
| GET | `/api/clients/lookup` | Staff: `Clientes.View` | Lookup operativo de clientes; acepta query `identification` y/o `phone`. |

No existe `POST /api/auth/register`. El rol `Cliente` no puede iniciar ni
renovar una sesión de plataforma.

## Portal de cliente retirado

| Método | Ruta | Acceso | Resultado |
| --- | --- | --- | --- |
| GET | `/api/clients/me` | Legacy | `410 Gone`. |
| GET | `/api/pets/mine` | Legacy | `410 Gone`. |
| POST | `/api/pets/mine` | Legacy | `410 Gone`. |
| PATCH | `/api/pets/mine/{petId:guid}` | Legacy | `410 Gone`. |
| GET | `/api/appointments/mine` | Legacy | `410 Gone`. |
| GET | `/api/appointments/mine/{appointmentId:guid}` | Legacy | `410 Gone`. |
| GET | `/api/appointments/booking/options` | Legacy | `410 Gone`. |
| GET | `/api/appointments/booking/slots` | Legacy | `410 Gone`. |
| POST | `/api/appointments/mine` | Legacy | `410 Gone`. |
| PATCH | `/api/appointments/mine/{id:guid}/cancel` | Legacy | `410 Gone`. |
| GET | `/api/accountstatements/mine` | Legacy | `410 Gone`. |
| GET | `/api/vaccinations/mine` | Legacy | `410 Gone`. |

La cancelación de cita sin portal permanece disponible mediante OTP:

| Método | Ruta | Acceso | Uso |
| --- | --- | --- | --- |
| POST | `/api/appointments/mine/{id:guid}/request-code` | Anónimo, rate limit | Solicita OTP para una acción de cita. |
| POST | `/api/appointments/mine/{id:guid}/confirm-code` | Anónimo, rate limit | Confirma el OTP y ejecuta la acción solicitada. |

## Operación de clínica

### Clientes, mascotas y catálogos veterinarios

| Método | Ruta | Acceso |
| --- | --- | --- |
| GET | `/api/clients` | `Clientes.View` |
| GET | `/api/clients/{id:guid}` | `Clientes.View` |
| POST | `/api/clients` | `Clientes.Create` |
| POST | `/api/clients/register-owner` | `Clientes.Create` |
| PUT | `/api/clients/{id:guid}` | `Clientes.Edit` |
| DELETE | `/api/clients/{id:guid}` | `Clientes.Delete` |
| GET | `/api/clientspets` | Staff |
| GET | `/api/clientspets/{id:guid}` | Staff |
| POST | `/api/clientspets` | `Mascotas.Create` |
| PUT | `/api/clientspets/{id:guid}` | `Mascotas.Edit` |
| DELETE | `/api/clientspets/{id:guid}` | `Mascotas.Delete` |
| GET | `/api/pets` | Staff |
| GET | `/api/pets/{id:guid}` | Staff |
| POST | `/api/pets` | `Mascotas.Create` |
| PUT | `/api/pets/{id:guid}` | `Mascotas.Edit` |
| DELETE | `/api/pets/{id:guid}` | `Mascotas.Delete` |
| GET | `/api/species` | Autenticado |
| GET | `/api/species/{id:guid}` | `Especies y Razas.View` |
| GET | `/api/species/{id:guid}/races` | Autenticado |
| POST | `/api/species` | `Especies y Razas.Create` |
| PUT | `/api/species/{id:guid}` | `Especies y Razas.Edit` |
| DELETE | `/api/species/{id:guid}` | `Especies y Razas.Delete` |
| GET | `/api/races` | Autenticado |
| GET | `/api/races/{id:guid}` | `Especies y Razas.View` |
| POST | `/api/races` | `Especies y Razas.Create` |
| PUT | `/api/races/{id:guid}` | `Especies y Razas.Edit` |
| DELETE | `/api/races/{id:guid}` | `Especies y Razas.Delete` |
| GET | `/api/specialties` | `Especialidades.View` |
| GET | `/api/specialties/{id:guid}` | `Especialidades.View` |
| POST | `/api/specialties` | `Especialidades.Create` |
| PUT | `/api/specialties/{id:guid}` | `Especialidades.Edit` |
| DELETE | `/api/specialties/{id:guid}` | `Especialidades.Delete` |
| GET | `/api/veterinarians/me` | `Veterinarios.View` |
| GET | `/api/veterinarians` | `Veterinarios.View` |
| GET | `/api/veterinarians/{id:guid}` | `Veterinarios.View` |
| POST | `/api/veterinarians` | `Veterinarios.Create` |
| PUT | `/api/veterinarians/{id:guid}` | `Veterinarios.Edit` |
| DELETE | `/api/veterinarians/{id:guid}` | `Veterinarios.Delete` |

### Servicios, citas y atención clínica

| Método | Ruta | Acceso |
| --- | --- | --- |
| GET | `/api/typeservices` | `Servicios.View` |
| GET | `/api/typeservices/{id:guid}` | `Servicios.View` |
| POST | `/api/typeservices` | `Servicios.Create` |
| PUT | `/api/typeservices/{id:guid}` | `Servicios.Edit` |
| DELETE | `/api/typeservices/{id:guid}` | `Servicios.Delete` |
| GET | `/api/services` | `Servicios.View` |
| GET | `/api/services/available` | Autenticado |
| GET | `/api/services/{id:guid}` | `Servicios.View` |
| POST | `/api/services` | `Servicios.Create` |
| PUT | `/api/services/{id:guid}` | `Servicios.Edit` |
| DELETE | `/api/services/{id:guid}` | `Servicios.Delete` |
| GET | `/api/availabilities` | Staff |
| GET | `/api/availabilities/available-slots` | Autenticado |
| GET | `/api/availabilities/{id:guid}` | Staff |
| GET | `/api/availabilities/by-veterinarian/{veterinarianId:guid}` | Staff |
| POST | `/api/availabilities` | `Citas.Create` |
| PUT | `/api/availabilities/{id:guid}` | `Citas.Edit` |
| DELETE | `/api/availabilities/{id:guid}` | `Citas.Delete` |
| GET | `/api/veterinarian-absences` | Staff |
| GET | `/api/veterinarian-absences/{id:guid}` | Staff |
| GET | `/api/veterinarian-absences/by-veterinarian/{veterinarianId:guid}` | Staff |
| POST | `/api/veterinarian-absences` | `Citas.Create` |
| PUT | `/api/veterinarian-absences/{id:guid}` | `Citas.Edit` |
| DELETE | `/api/veterinarian-absences/{id:guid}` | `Citas.Delete` |
| GET | `/api/appointments/me` | `Citas.View` |
| GET | `/api/appointments` | Staff |
| GET | `/api/appointments/{id:guid}` | Staff |
| POST | `/api/appointments` | `Citas.Create` |
| PATCH | `/api/appointments/{appointmentId:guid}/status` | `Citas.Edit` |
| PUT | `/api/appointments/{id:guid}` | `Citas.Edit` |
| DELETE | `/api/appointments/{id:guid}` | `Citas.Delete` |
| POST | `/api/appointments/{appointmentId:guid}/medical-record` | `Historiales Clínicos.Create` |
| GET | `/api/appointmentstatushistories` | Staff |
| GET | `/api/appointmentstatushistories/{id:guid}` | Staff |
| POST | `/api/appointmentstatushistories` | Staff clínico |
| PUT | `/api/appointmentstatushistories/{id:guid}` | Staff clínico |
| DELETE | `/api/appointmentstatushistories/{id:guid}` | Admin |
| GET | `/api/medicalrecords` | `Historiales Clínicos.View` |
| GET | `/api/medicalrecords/{id:guid}` | `Historiales Clínicos.View` |
| GET | `/api/diagnostics` | `Historiales Clínicos.View` |
| GET | `/api/diagnostics/{id:guid}` | `Historiales Clínicos.View` |
| POST | `/api/diagnostics` | `Historiales Clínicos.Create` |
| PUT | `/api/diagnostics/{id:guid}` | `Historiales Clínicos.Edit` |
| DELETE | `/api/diagnostics/{id:guid}` | `Historiales Clínicos.Delete` |
| GET | `/api/vaccinations` | `Historiales Clínicos.View` |
| GET | `/api/vaccinations/{id:guid}` | `Historiales Clínicos.View` |
| POST | `/api/vaccinations` | `Historiales Clínicos.Create` |
| PUT | `/api/vaccinations/{id:guid}` | `Historiales Clínicos.Edit` |

### Cobros, notificaciones, reportes y estados

| Método | Ruta | Acceso |
| --- | --- | --- |
| POST | `/api/accountstatements` | `Cuentas y Pagos.Create` |
| GET | `/api/accountstatements/{id:guid}` | Staff |
| GET | `/api/accountstatements/by-account/{accountId:guid}` | Staff |
| PATCH | `/api/accountstatements/{id:guid}/status` | `Cuentas y Pagos.Edit` |
| DELETE | `/api/accountstatements/{id:guid}` | `Cuentas y Pagos.Delete` |
| GET | `/api/notifications` | `Notificaciones.View` |
| GET | `/api/notifications/{id:guid}` | `Notificaciones.View` |
| GET | `/api/notifications/user/{userId:guid}` | `Notificaciones.View` |
| GET | `/api/notifications/appointment/{appointmentId:guid}` | `Notificaciones.View` |
| POST | `/api/notifications` | `Notificaciones.Create` |
| PUT | `/api/notifications/{id:guid}` | `Notificaciones.Edit` |
| DELETE | `/api/notifications/{id:guid}` | `Notificaciones.Delete` |
| GET | `/api/reports/appointments-by-veterinarian` | `Reportes.View` |
| GET | `/api/reports/appointments-by-day` | `Reportes.View` |
| GET | `/api/statusappointments` | `Estados de Cita.View` |
| GET | `/api/statusappointments/{id:guid}` | `Estados de Cita.View` |
| POST | `/api/statusappointments` | `Estados de Cita.Create` |
| PUT | `/api/statusappointments/{id:guid}` | `Estados de Cita.Edit` |
| DELETE | `/api/statusappointments/{id:guid}` | `Estados de Cita.Delete` |

## Administración de plataforma

| Método | Ruta | Acceso |
| --- | --- | --- |
| GET | `/api/users` | `Usuarios.View` |
| GET | `/api/users/{id:guid}` | `Usuarios.View` |
| POST | `/api/users` | `Usuarios.Create` |
| PUT | `/api/users/{id:guid}` | `Usuarios.Edit` |
| PATCH | `/api/users/{id:guid}/deactivate` | `Usuarios.Edit` |
| PATCH | `/api/users/{id:guid}/activate` | `Usuarios.Edit` |
| DELETE | `/api/users/{id:guid}` | `Usuarios.Delete` |
| GET | `/api/useraccounts` | `Usuarios.View` |
| GET | `/api/useraccounts/{id:guid}` | `Usuarios.View` |
| POST | `/api/useraccounts` | `Usuarios.Create` |
| PUT | `/api/useraccounts/{id:guid}` | `Usuarios.Edit` |
| DELETE | `/api/useraccounts/{id:guid}` | `Usuarios.Delete` |
| GET | `/api/usercredentials/{id:guid}` | `Usuarios.View` |
| GET | `/api/usercredentials/by-account/{accountId:guid}` | `Usuarios.View` |
| POST | `/api/usercredentials` | `Usuarios.Create` |
| PATCH | `/api/usercredentials/{id:guid}/change-password` | SuperAdmin |
| GET | `/api/usertokens/{id:guid}` | SuperAdmin |
| GET | `/api/usertokens/by-account/{accountId:guid}` | SuperAdmin |
| POST | `/api/usertokens` | SuperAdmin |
| DELETE | `/api/usertokens/{id:guid}` | SuperAdmin |
| GET | `/api/roles` | `Roles.View` |
| GET | `/api/roles/{id:guid}` | `Roles.View` |
| POST | `/api/roles` | `Roles.Create` |
| PUT | `/api/roles/{id:guid}` | `Roles.Edit` |
| DELETE | `/api/roles/{id:guid}` | `Roles.Delete` |
| GET | `/api/modules` | Admin |
| GET | `/api/modules/{id:guid}` | Admin |
| POST | `/api/modules` | Admin |
| PUT | `/api/modules/{id:guid}` | Admin |
| DELETE | `/api/modules/{id:guid}` | Admin |
| GET | `/api/role-permissions` | SuperAdmin |
| GET | `/api/role-permissions/{id:guid}` | SuperAdmin |
| GET | `/api/role-permissions/by-role/{roleId:guid}` | SuperAdmin |
| POST | `/api/role-permissions` | SuperAdmin |
| PUT | `/api/role-permissions/{id:guid}` | SuperAdmin |
| DELETE | `/api/role-permissions/{id:guid}` | SuperAdmin |
| GET | `/api/user-permissions` | SuperAdmin |
| GET | `/api/user-permissions/{id:guid}` | SuperAdmin |
| GET | `/api/user-permissions/by-user/{userId:guid}` | SuperAdmin |
| POST | `/api/user-permissions` | SuperAdmin |
| PUT | `/api/user-permissions/{id:guid}` | SuperAdmin |
| DELETE | `/api/user-permissions/{id:guid}` | SuperAdmin |

## Agente, Telegram y operaciones de dueño

| Método | Ruta | Acceso | Uso |
| --- | --- | --- | --- |
| POST | `/api/agent/messages` | Autenticado | Envía un mensaje al servicio de agente configurado. |
| POST | `/api/integrations/telegram/webhook` | Anónimo, rate limit + secreto de Telegram | Recibe actualizaciones de Telegram. |
| POST | `/api/integrations/telegram/link-codes` | Autenticado | Genera código/deep link temporal de vinculación. |
| GET | `/telegram/registration/complete` | Anónimo, rate limit + cookie temporal | Muestra el formulario de finalización de registro Telegram. |
| POST | `/telegram/registration/complete` | Anónimo, rate limit + cookie temporal | Completa el registro Telegram. |
| GET | `/api/bot/pets` | Bot interno | Lista mascotas del dueño delegado. |
| POST | `/api/bot/pets` | Bot interno | Registra una mascota del dueño delegado. |
| PATCH | `/api/bot/pets/{petId:guid}` | Bot interno | Actualiza una mascota del dueño delegado. |
| GET | `/api/bot/appointments` | Bot interno | Lista citas propias. |
| GET | `/api/bot/appointments/{appointmentId:guid}` | Bot interno | Consulta una cita propia. |
| GET | `/api/bot/appointments/booking/options` | Bot interno | Obtiene opciones para agendar. |
| GET | `/api/bot/appointments/booking/slots` | Bot interno | Consulta horarios disponibles. |
| POST | `/api/bot/appointments` | Bot interno | Agenda una cita; requiere `Idempotency-Key`. |
| PATCH | `/api/bot/appointments/{appointmentId:guid}/cancel` | Bot interno | Cancela una cita propia. |

## Administración conversacional e IA

Los endpoints de esta sección son `AdminOnly`, salvo que Swagger indique una
restricción adicional. Las rutas de lectura, alta, edición y borrado se listan
individualmente para mostrar el inventario completo.

| Método | Ruta |
| --- | --- |
| GET | `/api/ai/providers` |
| GET | `/api/ai/providers/{id:guid}` |
| POST | `/api/ai/providers` |
| PUT | `/api/ai/providers/{id:guid}` |
| PATCH | `/api/ai/providers/{id:guid}/activate` |
| PATCH | `/api/ai/providers/{id:guid}/deactivate` |
| GET | `/api/ai/models` |
| GET | `/api/ai/models/{id:guid}` |
| GET | `/api/ai/models/by-provider/{providerId:guid}` |
| POST | `/api/ai/models` |
| PUT | `/api/ai/models/{id:guid}` |
| PATCH | `/api/ai/models/{id:guid}/activate` |
| PATCH | `/api/ai/models/{id:guid}/deactivate` |
| GET | `/api/airunstatuses` |
| GET | `/api/airunstatuses/{id:guid}` |
| POST | `/api/airunstatuses` |
| PUT | `/api/airunstatuses/{id:guid}` |
| DELETE | `/api/airunstatuses/{id:guid}` |
| GET | `/api/conversationstatuses` |
| GET | `/api/conversationstatuses/{id:guid}` |
| POST | `/api/conversationstatuses` |
| PUT | `/api/conversationstatuses/{id:guid}` |
| DELETE | `/api/conversationstatuses/{id:guid}` |
| GET | `/api/escalationstatuses` |
| GET | `/api/escalationstatuses/{id:guid}` |
| POST | `/api/escalationstatuses` |
| PUT | `/api/escalationstatuses/{id:guid}` |
| DELETE | `/api/escalationstatuses/{id:guid}` |
| GET | `/api/messagetypes` |
| GET | `/api/messagetypes/{id:guid}` |
| POST | `/api/messagetypes` |
| PUT | `/api/messagetypes/{id:guid}` |
| DELETE | `/api/messagetypes/{id:guid}` |
| GET | `/api/priorities` |
| GET | `/api/priorities/{id:guid}` |
| POST | `/api/priorities` |
| PUT | `/api/priorities/{id:guid}` |
| DELETE | `/api/priorities/{id:guid}` |
| GET | `/api/sendertypes` |
| GET | `/api/sendertypes/{id:guid}` |
| POST | `/api/sendertypes` |
| PUT | `/api/sendertypes/{id:guid}` |
| DELETE | `/api/sendertypes/{id:guid}` |
| GET | `/api/chat/agent-humans` |
| GET | `/api/chat/agent-humans/{id:guid}` |
| GET | `/api/chat/agent-humans/by-user/{userId:guid}` |
| POST | `/api/chat/agent-humans` |
| PUT | `/api/chat/agent-humans/{id:guid}` |
| PATCH | `/api/chat/agent-humans/{id:guid}/activate` |
| PATCH | `/api/chat/agent-humans/{id:guid}/deactivate` |
| GET | `/api/chat/user-profiles` |
| GET | `/api/chat/user-profiles/{id:guid}` |
| GET | `/api/chat/user-profiles/by-user/{userId:guid}` |
| POST | `/api/chat/user-profiles` |
| PUT | `/api/chat/user-profiles/{id:guid}` |
| DELETE | `/api/chat/user-profiles/{id:guid}` |
| GET | `/api/chat/conversations` |
| GET | `/api/chat/conversations/{id:guid}` |
| POST | `/api/chat/conversations` |
| PATCH | `/api/chat/conversations/{id:guid}/status` |
| PATCH | `/api/chat/conversations/{id:guid}/priority` |
| PATCH | `/api/chat/conversations/{id:guid}/ai-enabled` |
| PATCH | `/api/chat/conversations/{id:guid}/close` |
| PATCH | `/api/chat/conversations/{id:guid}/reopen` |
| GET | `/api/chat/conversation-ai-settings` |
| GET | `/api/chat/conversation-ai-settings/{id:guid}` |
| GET | `/api/chat/conversation-ai-settings/by-conversation/{conversationId:guid}` |
| POST | `/api/chat/conversation-ai-settings` |
| PUT | `/api/chat/conversation-ai-settings/{id:guid}` |
| DELETE | `/api/chat/conversation-ai-settings/{id:guid}` |
| GET | `/api/chat/conversation-assignments` |
| GET | `/api/chat/conversation-assignments/{chatConversationId:guid}` |
| GET | `/api/chat/conversation-assignments/by-agent/{agentHumanId:guid}` |
| POST | `/api/chat/conversation-assignments` |
| PUT | `/api/chat/conversation-assignments/{chatConversationId:guid}` |
| DELETE | `/api/chat/conversation-assignments/{chatConversationId:guid}` |
| GET | `/api/chat/participants/{id:guid}` |
| GET | `/api/chat/participants/conversation/{chatConversationId:guid}` |
| POST | `/api/chat/participants` |
| PATCH | `/api/chat/participants/{id:guid}/identity` |
| GET | `/api/chat/messages/{id:guid}` |
| GET | `/api/chat/messages/conversation/{chatConversationId:guid}` |
| POST | `/api/chat/messages` |
| GET | `/api/chat/attachments/{id:guid}` |
| GET | `/api/chat/attachments/message/{chatMessageId:guid}` |
| POST | `/api/chat/attachments` |
| GET | `/api/chat/ai-runs/{id:guid}` |
| GET | `/api/chat/ai-runs/conversation/{chatConversationId:guid}` |
| POST | `/api/chat/ai-runs` |
| PATCH | `/api/chat/ai-runs/{id:guid}/status` |
| GET | `/api/chat/ai-run-metrics/{id:guid}` |
| GET | `/api/chat/ai-run-metrics/run/{chatAiRunId:guid}` |
| POST | `/api/chat/ai-run-metrics` |
| GET | `/api/chat/ai-run-errors/{id:guid}` |
| GET | `/api/chat/ai-run-errors/run/{chatAiRunId:guid}` |
| POST | `/api/chat/ai-run-errors` |
| GET | `/api/chat/escalations` |
| GET | `/api/chat/escalations/{id:guid}` |
| GET | `/api/chat/escalations/by-conversation/{chatConversationId:guid}` |
| POST | `/api/chat/escalations` |
| PUT | `/api/chat/escalations/{id:guid}` |
| DELETE | `/api/chat/escalations/{id:guid}` |
| GET | `/api/chat/escalation-assignments` |
| GET | `/api/chat/escalation-assignments/{id:guid}` |
| GET | `/api/chat/escalation-assignments/by-escalation/{chatEscalationId:guid}` |
| GET | `/api/chat/escalation-assignments/by-agent/{agentHumanId:guid}` |
| POST | `/api/chat/escalation-assignments` |
| PUT | `/api/chat/escalation-assignments/{id:guid}` |
| DELETE | `/api/chat/escalation-assignments/{id:guid}` |
| GET | `/api/chat/escalation-resolutions` |
| GET | `/api/chat/escalation-resolutions/{id:guid}` |
| GET | `/api/chat/escalation-resolutions/by-escalation/{chatEscalationId:guid}` |
| POST | `/api/chat/escalation-resolutions` |
| PUT | `/api/chat/escalation-resolutions/{id:guid}` |
| DELETE | `/api/chat/escalation-resolutions/{id:guid}` |
| GET | `/api/chat/escalation-status-histories` |
| GET | `/api/chat/escalation-status-histories/{id:guid}` |
| GET | `/api/chat/escalation-status-histories/by-escalation/{chatEscalationId:guid}` |
| POST | `/api/chat/escalation-status-histories` |
| PUT | `/api/chat/escalation-status-histories/{id:guid}` |
| DELETE | `/api/chat/escalation-status-histories/{id:guid}` |
