# Diseño del gateway de mensajería del agente

## Estado

Diseño aprobado el 28 de agosto de 2026 para el primer incremento de
comunicación operacional entre el backend .NET y Huellitas ChatBot.

## Objetivo

Exponer desde el backend .NET un endpoint autenticado que permita al usuario
enviar mensajes al agente FastAPI sin acceder directamente a ese servicio. El
incremento debe probar el flujo completo desde Swagger, mantener las
responsabilidades modulares y dejar fronteras sustituibles para la futura
persistencia de conversaciones, participantes, mensajes, ejecuciones de IA y
escalamientos.

Este incremento no convierte al módulo `Agent` en propietario del historial
conversacional. .NET seguirá siendo la autoridad y Oracle será el almacén
canónico cuando los módulos especializados estén disponibles.

## Decisiones arquitectónicas

### Módulo vertical Agent

El backend incorporará un módulo `Agent` distribuido por las capas existentes:

```text
src/
|-- Domain/
|   `-- Agent/
|       `-- Sin entidades en este incremento
|-- Application/
|   `-- Agent/
|       |-- Abstractions/
|       |-- Messages/
|       `-- Errors/
|-- Infrastructure/
|   `-- Agent/
|       |-- Http/
|       |-- Conversations/
|       `-- Configuration/
`-- Api/
    `-- Agent/
        |-- Controllers/
        `-- Dtos/
```

No se crearán entidades de dominio ficticias para completar la simetría. La
capa Domain participará cuando existan invariantes persistentes propias. Los
contratos HTTP de FastAPI serán privados de Infrastructure; Application solo
conocerá modelos y errores neutrales.

### Propiedad de persistencia

El esquema `veterinaria.mdj` separa las capacidades canónicas:

- `chat_conversations`: ciclo de vida de conversaciones.
- `chat_participants`: participantes y remitentes.
- `chat_messages`: historial canónico.
- `chat_ai_runs`: ejecuciones del agente.
- `chat_ai_run_metrics` y `chat_ai_run_errors`: métricas y errores.
- `chat_escalations`: escalamiento y asignación humana.
- `chat_conversation_ai_settings`: configuración de IA por conversación.

Estos modelos no pertenecerán a `Agent`. Sus futuros módulos implementarán los
puertos que `Agent` consume. El archivo `.mdj` es conceptual: antes de producir
migraciones Oracle se deberán normalizar tipos como `JSONB`, `TEXT` y `uuid` a
tipos Oracle aprobados, por ejemplo `JSON`, `CLOB` y `VARCHAR2(36)`.

### Identidad de conversaciones y canales

`conversationId` será siempre un UUID interno del backend. Un identificador de
Telegram, WhatsApp u otro canal no se utilizará como clave canónica.

La integración futura resolverá:

```text
(channel, externalChatId) -> conversationId interno
```

Los enlaces externos serán responsabilidad de conversaciones y canales. Un
futuro webhook de Telegram tendrá su propio adaptador de entrada, pero
reutilizará el mismo caso de uso de Application.

## Componentes

### Application

- `IAgentMessagingClient`: puerto neutral para solicitar una respuesta al
  agente.
- `IConversationContextProvider`: resuelve el UUID y, en el futuro, propiedad,
  canal, configuración de IA y estado de escalamiento.
- `IUserAccessTokenProvider`: permite reenviar el access token actual sin
  incluirlo en comandos, modelos de dominio ni logs.
- `SendAgentMessageCommand`: entrada autenticada del caso de uso.
- `SendAgentMessageHandler`: coordina identidad, contexto y cliente del agente.
- `AgentMessageResult`: resultado neutral que no expone DTOs de FastAPI.

### Infrastructure

- `AgentMessagingHttpClient`: adaptador HTTP tipado hacia FastAPI.
- Contratos privados que reflejan `POST /api/v1/messages`.
- `TransientConversationContextProvider`: sustituto temporal sin Oracle.
- `AgentOptions` y su validador de configuración.

### API

- `AgentMessagesController`.
- DTO público reducido y con propiedades desconocidas rechazadas.
- Lectura de `Idempotency-Key` y `X-Correlation-ID`.
- Autenticación JWT obligatoria.

## Contrato HTTP público

### Solicitud

```http
POST /api/agent/messages
Authorization: Bearer <access-token>
Idempotency-Key: <valor único obligatorio>
X-Correlation-ID: <UUID opcional>
Content-Type: application/json
```

```json
{
  "message": "¿Qué vacunas necesita mi mascota?",
  "conversationId": null,
  "petId": null,
  "language": "es-CO"
}
```

El contrato público no acepta `userId`, `roles`, `channel`, `isEscalated`,
`publishAsGlobalKnowledge`, proveedor, modelo ni metadatos de RAG. La identidad
procede del JWT validado por .NET.

### Solicitud interna a FastAPI

El adaptador construirá:

```json
{
  "message": "¿Qué vacunas necesita mi mascota?",
  "conversationId": "UUID resuelto",
  "userId": "person_id autenticado",
  "petId": null,
  "channel": "web",
  "language": "es-CO",
  "roles": ["rol autenticado"],
  "isEscalated": false,
  "correlationId": "UUID recibido o generado",
  "idempotencyKey": "valor del header",
  "publishAsGlobalKnowledge": false
}
```

El mismo access token se reenvía como Bearer para que FastAPI valide firma,
claims y coincidencia de identidad.

### Respuesta pública

```json
{
  "message": "Respuesta del agente",
  "conversationId": "UUID de la conversación",
  "correlationId": "UUID de seguimiento",
  "responseType": "ai_generated",
  "module": null
}
```

La API pública no expondrá proveedor, modelo, consumo de tokens, puntajes RAG,
IDs de Qdrant, URL interna ni cuerpos técnicos.

## Contexto temporal de conversación

Mientras no exista el módulo persistente:

- si llega `conversationId`, se conserva;
- si no llega, se genera un UUID;
- `channel` se fija como `web`;
- `isEscalated` se fija como `false`;
- no se escribe en Oracle;
- el resultado no se presenta como historial canónico.

Para mantener reintentos coherentes, el adaptador temporal conservará una
asociación acotada:

```text
(personId, idempotencyKey) -> conversationId
```

La asociación tendrá TTL, capacidad máxima y exclusión concurrente. Su pérdida
al reiniciar es una limitación explícita de desarrollo. El futuro proveedor
durable sustituirá este adaptador mediante DI sin modificar controlador ni caso
de uso.

## Configuración

La ubicación del agente y sus límites se configuran mediante entorno:

```dotenv
Agent__Enabled=true
Agent__BaseUrl=http://localhost:8000
Agent__MessagesPath=/api/v1/messages
Agent__RequestTimeoutSeconds=30
```

En una red Docker la base podrá cambiar a `http://agent-api:8000` sin modificar
código. Si `Agent__Enabled=true`, URL, ruta y timeout deberán ser válidos al
arrancar. `.env.example` no contendrá secretos ni claves reales.

## Flujo

```text
Usuario autenticado
    |
POST /api/agent/messages
    |
Validar JWT, body e Idempotency-Key
    |
Obtener personId, role y access token
    |
Resolver/generar conversationId temporal
    |
Construir contrato interno seguro
    |
AgentMessagingHttpClient -> FastAPI
    |
Traducir resultado o error neutral
    |
Responder desde .NET
```

No se mantendrá una transacción Oracle abierta durante una llamada HTTP. Cuando
exista persistencia, el orden será guardar y confirmar el mensaje del usuario,
llamar al agente y guardar después la respuesta o el error.

## Errores y resiliencia

| Condición | Respuesta pública |
|---|---|
| JWT ausente o inválido | `401` |
| Usuario sin autorización | `403` |
| Solicitud pública inválida | `400` |
| Idempotencia reutilizada con contenido distinto | `409` |
| Agente deshabilitado o no configurado | `503` |
| Red o FastAPI no disponible | `503 agent_unavailable` |
| Timeout | `504 agent_timeout` |
| FastAPI rechaza el contrato interno | `502 agent_contract_error` |
| FastAPI rechaza el JWT reenviado | `502 agent_authentication_error` |
| Conversación bajo control humano, en el futuro | `200 human_controlled` |

No se inventará una respuesta veterinaria cuando FastAPI falle. No habrá
reintentos automáticos ni circuit breaker en este incremento. El cliente puede
reintentar explícitamente con la misma clave idempotente.

El cliente tendrá timeout explícito, cancelación propagada y límite de tamaño
de respuesta. JWT, mensajes completos, secretos y cuerpos técnicos serán
redactados de logs y Problem Details.

## Pruebas

### Application

- Contexto nuevo cuando `conversationId` es nulo.
- Conservación de un ID recibido.
- Reutilización del ID con la misma clave idempotente.
- Identidad y rol obtenidos exclusivamente del principal autenticado.
- Construcción de contratos neutrales.
- Propagación de cancelación y errores.

### Infrastructure

- Uso de URL, ruta y timeout configurados.
- Reenvío exacto del Bearer sin registrarlo.
- Serialización compatible con FastAPI.
- Traducción de respuesta y Problem Details.
- Timeout, red caída, respuesta demasiado grande y respuestas inválidas.
- Ausencia de reintentos automáticos.

### API

- JWT obligatorio.
- DTO público sin campos de identidad o control interno.
- Rechazo de propiedades desconocidas.
- `Idempotency-Key` obligatorio y visible en Swagger.
- Correlation ID recibido o generado.
- `conversationId` siempre presente en respuestas exitosas.
- Errores seguros sin detalles internos.

Las pruebas automáticas usarán dobles y un servidor HTTP simulado; no llamarán
al agente, Oracle ni proveedores reales. La prueba manual optativa utilizará
Swagger .NET contra el contenedor local del chatbot.

## Fuera de alcance

- Migraciones y repositorios de conversaciones, participantes o mensajes.
- Persistencia de ejecuciones, métricas y errores de IA.
- Consulta real de escalamiento.
- Integraciones Telegram o WhatsApp.
- Redis, circuit breaker y reintentos automáticos.
- Módulos veterinarios ejecutables.
- Cambios en Huellitas ChatBot.
