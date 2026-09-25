# Configuración del canal Telegram

Esta integración recibe texto de chats privados y utiliza el mismo flujo del
módulo `Agent`. Persiste la conversación, el participante y el estado técnico
del webhook. Para un cliente **vinculado**, cada mensaje de texto que envía
también queda guardado en `CHAT_MESSAGES` (`SenderTypesId = Cliente`) — la
respuesta del agente de IA **no** se persiste todavía (diferido a propósito);
los mensajes de invitados sin vincular tampoco se guardan, ya que nunca
llegan a escalar.

Cuando un asesor humano responde desde la bandeja de Recepcionista
(`POST /api/chat/messages` con `SenderTypesId = Agente humano`), el backend
reenvía ese texto al mismo chat de Telegram automáticamente — ver
`ForwardHumanChatMessageToTelegramHandler`. Si el envío falla, el mensaje ya
quedó guardado igual: solo se pierde la entrega en tiempo real, no el
registro.

Un **cliente** es una fila en `CLIENTS` (sin `USERS`). El vínculo
`TELEGRAM_USER_LINKS` apunta al `clientId`. Contrato:
[`docs/contracts/clientes-usuarios-api-v2.md`](../contracts/clientes-usuarios-api-v2.md)
(secciones 7 y 9.1).

Si un cliente **ya vinculado** escribe una frase de escalamiento (por ejemplo
"asesor" o "hablar con alguien"), el backend crea un `ChatEscalation` en
estado Pendiente y responde con un mensaje fijo. Un **invitado sin vincular**
que escriba lo mismo se redirige al flujo de identificación del chatbot.

## 1. Preparar la configuración

Desde `@BotFather`, cree el bot y copie el token solamente en `.env`. Genere un
secreto aleatorio distinto del token. En desarrollo, abra un túnel HTTPS hacia
el puerto HTTPS del backend y copie su URL pública sin `/` final.

```dotenv
Telegram__Enabled=true
Telegram__GuestModeEnabled=true
Telegram__BotToken=<token entregado por BotFather>
Telegram__BotUsername=<nombre del bot sin @>
Telegram__WebhookSecret=secreto-aleatorio-con-letras-numeros-guion-o-guion-bajo
Telegram__PublicWebhookUrl=https://<url-publica-del-tunel>
Telegram__WorkerPollMilliseconds=30000
Telegram__WorkerConcurrency=16
Telegram__ProcessingLeaseSeconds=300
Telegram__MaxProcessingAttempts=3
Telegram__DelegatedTokenMinutes=5
Telegram__PendingEscalationStatusId=85000000-0000-0000-0000-000000000001
Telegram__TextMessageTypeId=83000000-0000-0000-0000-000000000001
Telegram__HumanAgentSenderTypeId=82000000-0000-0000-0000-000000000003
Telegram__OtpPepperBase64=<32 bytes aleatorios codificados en Base64>
Telegram__PrivateAccessAbsoluteTtlHours=24
Telegram__PrivateAccessIdleTtlMinutes=30
Telegram__RegistrationLinkWindowMinutes=10

Email__Enabled=true
Email__Host=<servidor SMTP>
Email__Port=587
Email__Username=<usuario SMTP>
Email__Password=<clave SMTP o clave de aplicación>
Email__FromAddress=no-reply@huellitas.example
Email__FromName=Huellitas
Email__UseTls=true
Serilog__MinimumLevel__Override__Microsoft.EntityFrameworkCore.Database.Command=Warning
```

El worker se despierta inmediatamente cuando el webhook guarda un update.
`Telegram__WorkerPollMilliseconds` es solamente el intervalo de respaldo.
`Telegram__WorkerConcurrency` define cuántos updates de **chats distintos**
se procesan en paralelo (1–32); el mismo chat sigue en serie.

Genere el pepper una sola vez para el ambiente:

```powershell
$otpPepper = New-Object byte[] 32
$randomGenerator = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $randomGenerator.GetBytes($otpPepper) } finally { $randomGenerator.Dispose() }
[Convert]::ToBase64String($otpPepper)
```

No cambie el pepper mientras existan verificaciones pendientes. Para Gmail u
otro proveedor con MFA utilice una clave de aplicación.

También deben estar configurados Oracle, JWT y `Agent__Enabled=true`. Las
migraciones las aplica la responsable del esquema; no genere migraciones en
tareas de producto.

## 2. Registrar el webhook

### Opción recomendada para pruebas locales: Cloudflare Quick Tunnel

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\start-telegram-cloudflare-tunnel.ps1
```

El script no inicia el backend. Tras la confirmación:

```powershell
dotnet run --project src/Api/Api.csproj --launch-profile http
```

### Opción manual

```powershell
$telegramBotToken = $env:Telegram__BotToken
$telegramWebhookSecret = $env:Telegram__WebhookSecret
$telegramPublicUrl = $env:Telegram__PublicWebhookUrl.TrimEnd('/')

$webhookBody = @{
  url = "$telegramPublicUrl/api/integrations/telegram/webhook"
  secret_token = $telegramWebhookSecret
  allowed_updates = @("message")
  drop_pending_updates = $true
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri "https://api.telegram.org/bot$telegramBotToken/setWebhook" `
  -ContentType "application/json" `
  -Body $webhookBody | Select-Object ok, description
```

## 3. Invitado, registro nuevo y `/start`

Con `Telegram__GuestModeEnabled=true`, cualquier chat privado puede hacer
preguntas generales. El backend usa la identidad técnica `TelegramGuest`.

`/start` y `/start <cualquier texto>` responden el mismo mensaje de
bienvenida de invitado. **No** hay consumo de códigos de vinculación
(`link-codes` está retirado).

Cuando el agente necesita datos del cliente, los recolecta en la conversación
y registra con `POST /api/owners/bot` → `{ clientId }`. Luego vincula con:

`POST /api/integrations/telegram/bot-link`  
Body: `{ "clientId": "<guid>" }`  
Auth: token de invitado (`TelegramGuestLinkOnly`, claim `telegram_user_id`).  
Respuesta: `{ "linkId": "<guid>" }`.

Ese `bot-link` directo aplica al **registro recién hecho** (ventana
`Telegram__RegistrationLinkWindowMinutes`, 10 por defecto). No crea usuario
fantasma en `USERS`: el cliente (dueño) nunca tiene fila en `USERS` ni
contraseña.

## 4. Reclamación por OTP (cliente existente, otro Telegram)

Aplica cuando alguien **ya es cliente** escribe desde un Telegram no vinculado.
El código se envía **siempre al correo registrado**, resuelto por el servidor
(el bot no elige el destino).

1. `POST /api/contact-verification/email/request-claim-by-identification`  
   Body `{ identificationNumber }` → **202**
   `{ sessionId, expiresAt, channel, maskedEmail }`.
2. `POST /api/contact-verification/email/confirm`  
   Body `{ sessionId, code }` → `{ sessionId, proof }`.
3. `POST /api/integrations/telegram/bot-link/claim`  
   Body `{ sessionId, proof }` → **200** `{ linkId, fullName }`.

`POST /api/contact-verification/email/request` **no** admite purpose `Claim`
(responde `ContactVerification.PurposeInvalid`). Si `bot-link` con
`{ clientId }` se usa fuera de la ventana de registro, espere
`403 Telegram.ClientLinkRequiresProof`.

Flujos **retirados** (no documentar como vigentes):
`POST /api/integrations/telegram/link-codes`,
`GET`/`POST /telegram/registration/complete`.

## 5. Diagnóstico rápido

- `401/403` en el webhook: revise `Telegram__WebhookSecret`.
- El webhook acumula pendientes: backend y Oracle; el worker solo se registra
  con `Telegram__Enabled=true`.
- El bot vincula pero no responde: `Agent__Enabled`, URL del agente y cliente
  activo.
- `getWebhookInfo.last_error_message` ayuda con túneles cerrados.

No registre en logs el token del bot, el secreto del webhook, JWT,
credenciales SMTP, correos o textos de usuarios.
