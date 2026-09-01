# Modo invitado de Telegram

## Objetivo

Permitir consultas veterinarias generales desde un chat privado de Telegram sin exigir vinculacion, conservando el OTP persistente como requisito para datos personales y futuras operaciones veterinarias.

## Limite de confianza

.NET continua siendo el unico emisor de JWT y la fuente de identidad. Para un chat no vinculado emite una identidad tecnica `TelegramGuest`, deterministica y aislada a partir de los identificadores de Telegram. Esta identidad no crea `Users`, `Clients`, participantes ni conversaciones en Oracle y no puede utilizar repositorios de mascotas, citas, vacunas o historias clinicas.

El token invitado conserva todos los claims exigidos por el agente y hace coincidir `person_id` con `userId`; no se relaja `identity_mismatch`. El agente reconoce exclusivamente el rol tecnico exacto `TelegramGuest`, deshabilita routing modular, evita publicacion global y aplica instrucciones de respuesta general. La vinculacion real conserva el flujo actual y cambia a la identidad, conversacion y autorizacion persistentes de la persona.

## Flujo

1. El webhook persiste y despierta el worker como hasta ahora.
2. Comandos `/vincular`, `/cancelar`, `/desvincular` y entradas de correo/OTP conservan prioridad.
3. Si existe `TelegramUserLink` activo, se ejecuta el flujo vinculado actual sin cambios.
4. Si no existe y `Telegram:GuestModeEnabled=false`, se conserva la respuesta estricta con `/vincular`.
5. Si no existe y el modo invitado esta activo, .NET crea identidad y conversacion UUID deterministicas en memoria y llama al mismo endpoint del agente.
6. El agente permite solo general/RAG, nunca ejecutores de modulo. La respuesta de Telegram agrega una invitacion breve a `/vincular` para personalizacion y operaciones.

`/start` sin codigo devuelve una bienvenida estatica para no consumir tokens. Explica que se puede preguntar como invitado y que `/vincular` habilita informacion personal. Un `/start <code>` conserva la vinculacion alternativa.

## Datos y privacidad

La conversacion invitada usa un UUID de espacio separado del vinculo real. Al vincularse se crea o reutiliza la conversacion persistente de la persona; no se copia memoria invitada ni se asocia retrospectivamente con la cuenta. Desvincular tampoco expone una conversacion anterior a una vinculacion posterior.

El RAG invitado puede leer conocimiento global y mantener memoria aislada por conversacion, pero nunca publicar como conocimiento global. JWT, IDs de Telegram, mensajes, respuestas y UUID invitados no se agregan a logs.

## Configuracion

`Telegram__GuestModeEnabled=false` es el valor seguro predeterminado. En el ambiente local se habilita explicitamente con `true`. El rol `TelegramGuest` es un contrato interno fijo, no un rol configurable ni una fila de Oracle.

## Aceptacion

- Un saludo invitado obtiene respuesta del agente y no crea registros conversacionales en Oracle.
- `/start` invitado explica ambos modos sin llamar al agente.
- `/vincular` y OTP funcionan igual que antes.
- Un usuario vinculado conserva su identidad, mascotas y conversacion persistente.
- El rol `TelegramGuest` nunca llama al router ni a ejecutores modulares.
- Un invitado no puede publicar conocimiento global y recibe una instruccion de vinculacion para operaciones privadas.
