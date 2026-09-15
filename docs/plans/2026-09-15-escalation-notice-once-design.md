# Aviso único durante escalamiento

## Contexto

Cuando una conversación de Telegram tiene un escalamiento activo, el backend envía
`Tu conversación está siendo atendida por un asesor.` después de cada mensaje del
cliente. El mensaje que inicia el escalamiento debe recibir esa confirmación, pero los
turnos siguientes deben llegar al asesor sin producir respuestas automáticas repetidas.

## Causa raíz

`ProcessTelegramUpdateHandler` conoce si la conversación ya estaba escalada mediante
`AgentConversationContext.IsEscalated`, pero continúa despachando cada turno al chatbot.
El chatbot responde `human_controlled` y el fallback del backend convierte una respuesta
vacía en el mismo aviso. Además, cuando el usuario repite una frase de escalamiento,
`EscalateAsync` evita duplicar el registro, pero el handler igualmente envía la
confirmación.

## Comportamiento aprobado

- El mensaje que crea un escalamiento persiste, crea el registro y recibe una única
  confirmación por Telegram.
- Mientras ese escalamiento siga activo, cada mensaje posterior se persiste y queda
  disponible para el asesor, pero no se despacha al chatbot ni genera respuesta de bot.
- Repetir una frase como `asesor` durante el mismo escalamiento tampoco vuelve a mostrar
  la confirmación.
- Al resolver el escalamiento, el procesamiento automático vuelve a funcionar.
- Un escalamiento nuevo, posterior a una resolución, vuelve a enviar una confirmación.

## Diseño

`TelegramInboundUpdate` incorporará una transición explícita para completar un update en
estado `Processing` sin preparar ni enviar texto. La transición conservará las mismas
garantías de limpieza que `Complete`: estado final `Completed`, texto sensible removido,
respuesta y error eliminados, y fecha de actualización registrada.

Después de persistir el mensaje del cliente, `ProcessTelegramUpdateHandler` comprobará
`context.IsEscalated`. Si es verdadero, completará el update silenciosamente y terminará.
La comprobación ocurre antes de detectar nuevas frases de escalamiento y antes del
dispatcher, por lo que no crea duplicados ni invoca IA. Cuando es falso, el flujo actual
permanece intacto: una solicitud de asesor crea el escalamiento y envía la confirmación.

## Capas e impacto

- Domain: nueva transición de estado sin atributos persistidos nuevos.
- Application: corte temprano del flujo escalado y persistencia del update completado.
- Infrastructure: sin cambios; EF ya persiste las propiedades existentes.
- Api: sin cambios de rutas, contratos ni autorización.
- Base de datos: sin migración ni seeders.

## Errores e idempotencia

El update solo puede completarse silenciosamente desde `Processing`; invocarlo desde otro
estado falla con `InvalidOperationException`. Una vez en `Completed`, el procesamiento
existente ignora reintentos del mismo update. Los fallos al persistir el mensaje conservan
el mecanismo actual de reintentos y no marcan el update como completado prematuramente.

## Pruebas

- Transición de dominio desde `Processing` hasta `Completed` y limpieza de datos.
- Rechazo de la transición desde un estado inválido.
- Conversación ya escalada: persiste el mensaje, no llama al dispatcher, no crea otro
  escalamiento y no envía texto por Telegram.
- Primera solicitud de asesor: mantiene la única confirmación existente.
- Conversación no escalada: conserva el despacho normal.

