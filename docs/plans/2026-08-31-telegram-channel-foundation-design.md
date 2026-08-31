# Diseño de la integración del canal Telegram

## Objetivo

Conectar un bot de Telegram con el backend modular para recibir mensajes de
texto, resolver de forma segura al usuario de Huellitas, reutilizar el caso de
uso del módulo `Agent` y devolver la respuesta al chat de Telegram.

Esta primera fase debe dejar funcional el recorrido completo sin persistir el
historial de mensajes. Solo se conservarán el perfil de chat, la conversación,
el participante y los vínculos técnicos necesarios para Telegram. El guardado
de mensajes en `CHAT_MESSAGES` se implementará posteriormente.

## Alcance aprobado

- Recepción mediante webhook HTTPS.
- Túnel HTTPS de VS Code durante desarrollo local.
- Dominio HTTPS del VPS en producción.
- Chats privados exclusivamente.
- Mensajes de texto exclusivamente.
- Vinculación segura de una cuenta autenticada mediante código temporal.
- Una vinculación activa de Telegram por cuenta Huellitas en esta fase.
- Procesamiento asíncrono con bandeja técnica persistente y worker.
- Creación y reutilización de conversaciones y participantes existentes.
- Nueva conversación cuando la conversación vinculada anterior esté cerrada.
- JWT RS256 delegado y de corta duración para la llamada al agente.
- Respuesta mediante `sendMessage` de Telegram.
- Sin escritura en `CHAT_MESSAGES`.

Quedan fuera de alcance imágenes, audio, documentos, grupos, edición de
mensajes, callbacks, historial conversacional y atención humana desde
Telegram.

## Límites arquitectónicos

`Telegram` será un módulo de integración del monolito y no formará parte del
módulo `Agent`.

- `Telegram` conoce el contrato externo de Telegram, la vinculación, los IDs
  externos, la bandeja técnica y el envío de respuestas.
- `Agent` conserva la coordinación neutral con el chatbot FastAPI.
- `ChatUserProfiles`, `ChatConversations` y `ChatParticipants` conservan la
  persistencia especializada del dominio conversacional.
- El módulo Telegram depende de puertos de Application y nunca accede al
  `DbContext` desde un controlador o worker.
- Los identificadores de Telegram nunca reemplazan el UUID interno de una
  conversación.

La relación canónica será:

```text
(telegram, telegramChatId) -> conversationId interno
```

## Componentes

### API

`POST /api/integrations/telegram/link-codes`

- Requiere el JWT normal del usuario.
- Deriva `personId` del token; no lo acepta desde el cuerpo.
- Invalida códigos pendientes anteriores.
- Devuelve el código una sola vez, su vencimiento y el deep link del bot.

`POST /api/integrations/telegram/webhook`

- Es anónimo respecto al JWT público.
- Exige `X-Telegram-Bot-Api-Secret-Token`.
- Valida el contrato mínimo, inserta idempotentemente el `update_id` y responde
  inmediatamente con un estado `2xx`.
- No llama directamente al agente ni a Telegram.

### Application

- Caso de uso para emitir códigos de vinculación.
- Caso de uso para consumir `/start <code>` de forma atómica.
- Coordinación del procesamiento de una actualización.
- Resolución del vínculo del usuario y del chat externo.
- Resolución o creación de conversación y participante mediante los puertos
  existentes.
- Dispatcher común del agente reutilizable por Web/JWT y Telegram/worker.
- Puertos para bandeja, vínculos, cliente de Telegram, token delegado y reloj.

### Infrastructure

- Repositorios EF Core/Oracle del módulo Telegram.
- Cliente HTTP tipado para Telegram Bot API.
- Worker que reclama trabajos pendientes y aplica reintentos limitados.
- Emisor de JWT delegado apoyado en la infraestructura RS256 existente.
- Registro y validación de opciones por variables de entorno.

### Agent

Se extraerá la coordinación común del handler actual a un dispatcher. El
endpoint web conservará su JWT real. El worker de Telegram obtendrá un token
delegado después de comprobar el vínculo y el estado activo de la cuenta.
Ambos recorridos utilizarán la misma resolución de conversación,
escalamiento, RAG, idempotencia y cliente FastAPI.

## Vinculación de identidad

1. El usuario autenticado solicita un código desde Swagger.
2. El backend genera un valor criptográficamente aleatorio, almacena solo su
   hash y lo limita a diez minutos.
3. El usuario abre `https://t.me/<bot>?start=<code>`.
4. Telegram entrega el comando al webhook.
5. El worker consume el código una sola vez y vincula `telegramUserId` y
   `telegramChatId` con el `personId` autenticado que originó el código.
6. Los mensajes posteriores se autorizan mediante ese vínculo, nunca mediante
   el nombre, username o teléfono declarado en Telegram.

Los códigos vencidos, usados o desconocidos producen una respuesta controlada
y no llaman al agente.

## Modelo persistente

### `TELEGRAM_LINK_CODES`

- `id`: UUID.
- `person_id`: FK al usuario del sistema.
- `code_hash`: hash único; el valor original nunca se almacena.
- `expires_at`.
- `consumed_at` nullable.
- auditoría de creación y actualización necesaria.

### `TELEGRAM_USER_LINKS`

- `id`: UUID.
- `person_id`: vínculo único para esta primera fase.
- `telegram_user_id`: entero de 64 bits y único.
- `telegram_chat_id`: entero de 64 bits y único para chat privado.
- `linked_at` y `updated_at`.

### `TELEGRAM_CONVERSATION_LINKS`

- `id`: UUID.
- `telegram_user_link_id`: FK única al vínculo activo.
- `conversation_id`: FK a la conversación interna.
- `created_at` y `updated_at`.

Si la conversación está abierta o escalada se reutiliza. Si está cerrada se
crea una conversación nueva y se actualiza este vínculo.

### `TELEGRAM_INBOUND_UPDATES`

- `update_id`: identificador idempotente de Telegram.
- `telegram_user_id`, `telegram_chat_id` y `telegram_message_id`.
- estado técnico: pendiente, procesando, completado o fallido.
- número de intentos y próxima fecha de ejecución.
- texto de entrada y respuesta exclusivamente temporales.
- índice del último fragmento de salida enviado.
- fechas técnicas y descripción segura del último error.

Al completar el flujo se limpian los textos de entrada y salida. Esta tabla no
es historial ni sustituye `CHAT_MESSAGES`.

Los IDs externos se representan con `long` en C# y `NUMBER(19)` en Oracle.

## Flujo de mensajes

1. Telegram realiza el POST del webhook.
2. API compara el secreto de forma segura.
3. Inserta el `update_id` si no existe y responde inmediatamente.
4. El worker reclama el trabajo de manera atómica.
5. Rechaza de forma controlada chats no privados y contenido no textual.
6. Atiende `/start` como vinculación, sin llamar al agente.
7. Para texto normal, resuelve el usuario vinculado y valida que siga activo.
8. Resuelve la conversación abierta; si no existe o está cerrada, crea el
   perfil cuando sea necesario, la conversación y el participante Cliente.
9. Emite un JWT delegado RS256 de cinco minutos.
10. Invoca el dispatcher con `channel = "telegram"` y la clave determinista
    `telegram-update-<updateId>`.
11. Guarda temporalmente el resultado preparado.
12. Divide respuestas mayores de 4096 caracteres en fragmentos de texto plano
    y los envía en orden mediante `sendMessage`.
13. Marca el trabajo como completado y limpia los textos temporales.

## Consistencia y concurrencia

- La inserción del webhook es idempotente por `update_id`.
- El reclamo del worker debe impedir que dos instancias procesen el mismo
  trabajo al mismo tiempo.
- La creación de conversación, participante y vínculo externo comparte una
  única transacción de base de datos.
- Las llamadas a FastAPI y Telegram ocurren fuera de transacciones Oracle.
- Mensajes simultáneos del mismo chat deben serializarse para no crear dos
  conversaciones iniciales.
- Todos los reintentos al agente reutilizan la misma idempotency key.
- Telegram no ofrece una clave idempotente para `sendMessage`; se conserva el
  progreso por fragmento para reducir duplicados, aunque existe una ventana
  inevitable si el proceso cae después del envío y antes de confirmar en
  Oracle.

## Seguridad

- Token del bot, secreto del webhook y claves JWT permanecen en variables de
  entorno.
- El secreto del webhook no se incluye en la URL ni en logs.
- No se registran JWT, códigos, mensajes, respuestas ni datos personales.
- El webhook solo admite el contrato mínimo necesario.
- El código es aleatorio, de un solo uso, de corta duración y se persiste como
  hash.
- La identidad delegada se emite solo para una cuenta vinculada y activa.
- El endpoint de códigos usa la autorización autenticada ordinaria, no una
  política administrativa.
- El webhook tiene autenticación propia por secreto y un límite de solicitudes
  específico.

## Errores y reintentos

- Secreto inválido: rechazo sin detalles internos.
- Update duplicado: respuesta exitosa sin una segunda ejecución.
- Usuario sin vínculo: instrucción para generar el código.
- Código inválido, usado o vencido: respuesta controlada.
- Tipo de chat o contenido no soportado: respuesta controlada.
- Fallo temporal del agente o Telegram: máximo de intentos configurable con
  espera incremental.
- Fallo definitivo del agente: estado fallido y respuesta genérica si Telegram
  continúa disponible.
- Conversación escalada: se respeta el resultado actual sin forzar una llamada
  al modelo.

## Configuración

```env
Telegram__Enabled=true
Telegram__BotToken=
Telegram__BotUsername=
Telegram__WebhookSecret=
Telegram__PublicWebhookUrl=
Telegram__LinkCodeTtlMinutes=10
Telegram__WorkerPollMilliseconds=1000
Telegram__ProcessingLeaseSeconds=300
Telegram__MaxProcessingAttempts=3
Telegram__DelegatedTokenMinutes=5
```

La configuración se valida al iniciar solo cuando Telegram está habilitado.
El registro mediante `setWebhook` será una operación explícita documentada y
no un efecto lateral de cada inicio del contenedor.

### Variables retiradas del contexto transitorio

Las siguientes variables no deben volver a añadirse al `.env` ni a
`.env.example`:

```env
Agent__ConversationContextTtlSeconds=900
Agent__ConversationContextCapacity=10000
```

Ambas configuraban exclusivamente el proveedor temporal de conversaciones en
memoria. La rama de contexto persistente sustituyó ese proveedor por Oracle,
por lo que ya no existe una capacidad local ni un TTL que aplicar. README debe
documentar esta retirada para evitar que se interprete como una omisión y debe
señalar como reemplazo funcional `Agent__InitialConversationStatusId` y
`Agent__ClientParticipantTypeId`.

## Verificación

Las pruebas focalizadas cubrirán:

- emisión, expiración, consumo e invalidación de códigos;
- autorización del endpoint de códigos;
- secreto del webhook y contratos no compatibles;
- idempotencia por `update_id`;
- vínculo seguro con cuenta activa;
- creación inicial de perfil, conversación y participante;
- reutilización de conversación abierta o escalada;
- nueva conversación después del cierre;
- emisión y validación del token delegado;
- reintentos y fallos definitivos;
- división de mensajes largos y progreso de fragmentos;
- ausencia de escrituras en `CHAT_MESSAGES`;
- OpenAPI, compilación y pruebas de los conjuntos afectados.

La prueba manual final utilizará un túnel HTTPS de VS Code, `setWebhook`, un
código generado desde Swagger y un mensaje privado real al bot.
