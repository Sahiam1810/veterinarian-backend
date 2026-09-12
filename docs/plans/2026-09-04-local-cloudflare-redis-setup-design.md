# Configuración local de Cloudflare y Redis

## Objetivo

Permitir pruebas locales de Telegram con un Quick Tunnel de Cloudflare y ejecutar el
agente Docker con Redis como runtime y almacén de checkpoints, sin iniciar el backend
automáticamente ni guardar secretos en Git.

## Diseño

El agente conserva Redis como adaptador desacoplado. Su `.env` privado selecciona
`redis://redis:6379`, base lógica `0`, checkpoints Redis y expiración de siete días.
Docker Compose continúa siendo dueño del contenedor Redis, su volumen AOF y la red
interna; no se añade autenticación porque el puerto está limitado a localhost y esta
configuración es solo de desarrollo.

El entorno local incorpora un script PowerShell explícito e ignorado por Git. El script inicia `cloudflared` en
segundo plano apuntando a `http://localhost:5233`, espera la URL HTTPS temporal,
actualiza únicamente `Telegram__PublicWebhookUrl` en el `.env` privado y registra el
webhook con los secretos que ya existen allí. El script no inicia el backend y permanece
ejecutándose para mantener vivo el túnel; al cerrarlo también termina `cloudflared`.

## Flujo de uso

1. Ejecutar el script del túnel en una terminal.
2. Esperar la confirmación de URL y webhook.
3. Iniciar manualmente el backend en otra terminal.
4. Mantener abiertas ambas terminales durante la prueba.
5. Ejecutar el agente con Docker Compose; `agent-api` utiliza Redis por nombre de servicio.

## Seguridad y errores

- El token del bot y el secreto del webhook nunca se imprimen.
- El script operativo y su prueba auxiliar quedan ignorados por Git y solo existen en
  la estación local del desarrollador.
- El `.env` continúa ignorado por Git.
- Una URL no obtenida, un ejecutable ausente o una respuesta fallida de Telegram detienen
  el script con un mensaje accionable y cierran el proceso del túnel.
- Reiniciar el Quick Tunnel genera otra URL; el mismo script vuelve a actualizarla y
  registrar el webhook.

## Verificación

- Validar sintaxis PowerShell sin abrir el túnel.
- Validar configuración de Docker Compose.
- Validar que `Settings` seleccione Redis y checkpoints Redis dentro del contenedor.
- Confirmar que ningún secreto ni `.env` aparezca en los cambios versionados.
