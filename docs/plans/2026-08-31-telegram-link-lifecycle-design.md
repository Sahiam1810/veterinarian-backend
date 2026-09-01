# Ciclo de vida de vinculacion de Telegram

## Objetivo

Permitir que un chat previamente desvinculado complete nuevamente el flujo OTP sin perder el historial tecnico, impedir que usuarios no vinculados invoquen al agente y reducir el sondeo repetitivo de Oracle sin aumentar la latencia normal del webhook.

## Diagnostico confirmado

La desvinculacion actual establece `UNLINKED_AT`, pero los indices unicos fisicos de `TELEGRAM_USER_LINKS` siguen incluyendo filas revocadas. Al completar un OTP nuevo, el servicio intenta insertar una fila y Oracle responde `ORA-00001` sobre `UX_TELEGRAM_USER_LINKS_CHAT`.

## Diseno aprobado

### Vinculos e historial

`TELEGRAM_USER_LINKS` conserva cada vinculacion como registro historico. Los indices unicos de persona, usuario de Telegram y chat se reemplazan por indices unicos funcionales de Oracle que solo indexan filas con `UNLINKED_AT IS NULL`. Una fila revocada deja de reservar esos identificadores, pero continua disponible para las relaciones e historial existentes.

Antes de completar el OTP, Application consulta los vinculos activos por persona, usuario y chat. Un vinculo activo perteneciente a otra persona produce una respuesta funcional controlada y cancela la sesion; unicamente los vinculos revocados permiten crear o reactivar la vinculacion solicitada. La restriccion de base de datos continua siendo la ultima defensa ante concurrencia.

La migracion sera aditiva respecto de los datos: elimina solamente los tres indices unicos anteriores y crea sus equivalentes condicionales. No elimina filas ni modifica claves foraneas. No se aplicara automaticamente a una base sin confirmacion.

### Acceso al agente

Todo mensaje de texto de un chat privado no vinculado, incluidos `/start` sin codigo y saludos, recibe una bienvenida breve que indica enviar `/vincular`. El flujo OTP consume correo y codigo sin invocar al agente. Solo una vinculacion activa permite resolver la conversacion y llamar al servicio del agente.

### Activacion del worker

El webhook persiste primero el update y despues publica una senal interna coalescente. El worker espera esa senal y procesa inmediatamente; si la senal se pierde o el proceso recibe trabajo de otra instancia, conserva un sondeo de respaldo configurable. El valor documentado pasa de 5 segundos a 30 segundos.

La senal se define como un puerto de Application y se implementa en Infrastructure con un canal acotado de capacidad uno. No se introduce Redis ni se cambia el contrato HTTP. Para un despliegue con varias replicas, el sondeo de respaldo preserva correccion; una senal distribuida puede sustituir el adaptador posteriormente.

Los logs de comandos EF permanecen en nivel `Warning`, por lo que las consultas de sondeo exitosas no deben imprimirse en ejecucion normal.

## Manejo de errores

- OTP invalido o vencido conserva las respuestas seguras existentes.
- Persona, usuario o chat vinculados activamente a otra identidad producen conflicto controlado y no se reintentan como error tecnico.
- Una colision concurrente de unicidad se traduce a conflicto de identidad; no deja el update en un ciclo de reintentos inesperados.
- Un error real de Oracle, Telegram o del agente mantiene el mecanismo acotado de reintentos existente.

## Pruebas y aceptacion

- Una fila revocada no bloquea una nueva vinculacion para el mismo chat.
- Un vinculo activo ajeno no puede ser reemplazado mediante OTP.
- Desvincular conserva la fila y libera persona, usuario y chat para una vinculacion futura.
- Un saludo no vinculado responde con `/vincular` y no llama al agente.
- Ingerir un update aceptado despierta al worker; un duplicado no genera trabajo adicional.
- Sin senales, el worker vuelve a consultar al cumplirse el intervalo de respaldo.
- Se ejecutan solo pruebas Telegram dirigidas, build y comprobacion de cambios pendientes del modelo.

