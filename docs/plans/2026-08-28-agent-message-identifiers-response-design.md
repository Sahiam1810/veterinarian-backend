# Identificadores automáticos y respuesta completa del agente

## Objetivo

Permitir que `POST /api/agent/messages` funcione sin exigir al consumidor los encabezados de idempotencia y correlación, conservando la posibilidad de que clientes avanzados los proporcionen, y devolver el contrato completo que entrega el agente FastAPI.

## Contrato aprobado

- `Idempotency-Key` será opcional. Si no se recibe o contiene solo espacios, el backend generará una clave con formato `msg-{guid-en-formato-N}`.
- `X-Correlation-ID` será opcional. Si no se recibe, el backend generará un `Guid` no vacío.
- Los valores proporcionados por el consumidor se conservarán sin reemplazarlos.
- La generación será local a la petición y no usará Oracle. La idempotencia durable entre procesos queda fuera de alcance hasta que exista el módulo persistente de conversaciones y mensajes.
- La respuesta HTTP incluirá `message`, `conversationId`, `correlationId`, `responseType`, `provider`, `model`, `usage`, `module` y `rag`.
- `provider`, `model`, `usage`, `module` y `rag.topScore` admitirán `null` de acuerdo con el contrato real del agente.
- `rag` incluirá `status`, `route`, `topScore`, `globalMatches`, `conversationMatches`, `memoryStored` y `knowledgePublished`.

## Arquitectura y flujo

El controlador seguirá obteniendo `person_id` y `role` exclusivamente del JWT. Antes de crear el comando normalizará o generará los identificadores opcionales. Application continuará recibiendo identificadores ya resueltos y propagará un resultado tipado completo. Infrastructure deserializará la respuesta de FastAPI y la mapeará al contrato neutral de Application. API mapeará ese resultado a DTOs públicos anidados para que Swagger documente la forma completa.

No se modifica Domain ni se introduce persistencia. Tampoco se cambia el cuerpo de la petición ni la ruta existente.

## Compatibilidad y seguridad

- Los consumidores actuales que envían ambos encabezados mantendrán el mismo comportamiento.
- Los consumidores nuevos podrán omitirlos.
- La autorización permanece como `authenticated-fallback` mediante `[Authorize]`.
- Se mantienen las verificaciones de coincidencia de `conversationId` y `correlationId` entre solicitud y respuesta.
- Los tipos de proveedor, modelo, ruta y estado se transportarán como texto para tolerar nuevas variantes del agente sin desplegar simultáneamente el backend.
- La respuesta ampliada expone metadatos operativos solicitados, pero no incluye tokens de acceso, claves ni contenido interno adicional.

## Errores

La generación local no agrega resultados de error. Se conservan las traducciones actuales para autenticación, conflicto de idempotencia, contrato inválido, indisponibilidad y timeout del agente. Una respuesta exitosa que no pueda deserializar el contrato completo se tratará como `AgentContractException`.

## Pruebas

- API genera ambos identificadores cuando se omiten y los propaga al agente.
- API conserva los encabezados cuando se proporcionan.
- OpenAPI presenta ambos encabezados como opcionales y documenta la respuesta completa.
- Infrastructure deserializa todos los campos y preserva los valores nullable.
- API serializa `usage` y `rag` con nombres camelCase.
- Las comprobaciones existentes de identidad, autorización, tamaño, cancelación y errores continúan pasando.

