# Contexto persistente de conversaciones del agente

## Objetivo

Conectar `POST /api/agent/messages` con los módulos existentes de conversaciones, perfiles conversacionales y participantes para que cada intercambio se asocie con una conversación real en Oracle y el backend pueda validar quién participa en ella.

## Alcance

- Crear una conversación cuando el primer mensaje no incluya `conversationId`.
- Resolver el `ChatUserProfile` del usuario autenticado y crearlo cuando no exista.
- Registrar al perfil como participante de tipo `Cliente`.
- Validar existencia y pertenencia cuando se reciba un `conversationId`.
- Consultar el estado de escalamiento de la conversación antes de llamar al agente.
- Enviar al chatbot el identificador persistido y el indicador de escalamiento.
- Incorporar un seed SQL idempotente para los catálogos mínimos.

Quedan fuera de alcance la persistencia de mensajes, la creación del participante IA, la asignación de agentes humanos y cualquier endpoint nuevo.

## Catálogos y configuración

Se agregará `database/seeds/chat_conversation_catalogs_seed.sql`, siguiendo el patrón operativo de `roles_seed.sql`. Usará `MERGE` de Oracle con identificadores deterministas para que pueda ejecutarse repetidamente sin duplicar datos.

El seed incluirá inicialmente:

- Estado de conversación `Abierta`.
- Tipo de participante `Cliente`.

Los identificadores se configurarán mediante:

- `Agent__InitialConversationStatusId`.
- `Agent__ClientParticipantTypeId`.

La aplicación validará la sintaxis de ambos GUID al arrancar y comprobará la existencia de los registros cuando deba crear el contexto. De esta manera, la lógica no dependerá de nombres editables ni de identificadores ocultos en el código.

## Flujo sin `conversationId`

1. `AgentMessagesController` deriva `person_id` del JWT y conserva la generación actual de correlación e idempotencia.
2. `SendAgentMessageHandler` solicita el contexto conversacional.
3. El proveedor persistente busca perfiles asociados al `UserId` autenticado.
4. Si no existe un perfil, crea uno con sus campos opcionales vacíos.
5. Crea una `ChatConversation` con el estado inicial configurado y la IA habilitada.
6. Crea un `ChatParticipant` enlazado con la conversación, el perfil y el tipo `Cliente`.
7. Persiste perfil, conversación y participante mediante un único `SaveChangesAsync`.
8. Devuelve el ID real al flujo existente, que lo envía al chatbot.

## Flujo con `conversationId`

1. Buscar la conversación; responder con recurso no encontrado si no existe.
2. Resolver los perfiles del usuario autenticado.
3. Cargar los participantes de la conversación.
4. Autorizar el acceso únicamente si uno de esos participantes corresponde a un perfil del usuario.
5. Rechazar como prohibido cualquier intento de usar una conversación ajena.
6. Determinar si existe un escalamiento activo y transmitir `isEscalated` al chatbot.

No se aceptará silenciosamente un identificador inexistente ni se creará una conversación utilizando un ID proporcionado por el cliente.

## Arquitectura

`IConversationContextProvider` seguirá siendo el puerto consumido por el módulo Agent. La implementación temporal se reemplazará por una implementación persistente registrada con ciclo de vida `Scoped`, compatible con `VeterinaryDbContext` e `IUnitOfWork`.

La operación coordinará los repositorios especializados existentes; no llamará los controladores administrativos de conversaciones o participantes y no accederá directamente a Oracle desde la capa API.

## Atomicidad y concurrencia

El alta inicial agregará las entidades al mismo contexto EF y realizará un solo `SaveChangesAsync`, aprovechando la transacción automática de EF Core. Si falla la creación de cualquier relación, no debe quedar una conversación parcial.

La clave de idempotencia continuará propagándose al chatbot, pero el esquema actual no permite asociarla de manera durable con una conversación creada. Por ello, los reintentos posteriores a una primera respuesta deberán enviar el `conversationId` retornado. La idempotencia durable de la creación queda fuera de este alcance y deberá abordarse con un almacén compartido antes de operar varias instancias del backend.

## Errores

- Configuración inválida: fallo de validación al arrancar.
- Catálogo configurado inexistente: error de configuración operativo sin llamar al chatbot.
- Conversación inexistente: `404`.
- Usuario no participante: `403`.
- Error de persistencia: respuesta de error sin ejecutar el agente.
- Conversación escalada: se conserva el flujo humano definido por el agente y no se invoca el modelo.

No se incluirán JWT, mensajes ni datos personales en logs.

## Pruebas

Se seguirá TDD con pruebas focalizadas para:

- creación de perfil, conversación y participante;
- reutilización del perfil existente;
- validación de los GUID de configuración;
- conversación existente perteneciente al usuario;
- rechazo de conversación ajena o inexistente;
- cálculo de escalamiento;
- integración HTTP del contrato actual;
- registro `Scoped` del proveedor persistente.

La verificación final incluirá las pruebas focalizadas, compilación completa, `git diff --check` y revisión del diff. No se aplicarán migraciones ni seeds automáticamente sobre Oracle.
