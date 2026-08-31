# Configuración del canal Telegram

Esta integración recibe texto de chats privados, vincula la identidad de
Telegram con una cuenta Huellitas y utiliza el mismo flujo del módulo `Agent`.
En esta fase persiste la conversación, el participante y el estado técnico del
webhook, pero no guarda el historial en `CHAT_MESSAGES`.

## 1. Preparar la configuración

Desde `@BotFather`, cree el bot y copie el token solamente en `.env`. Genere un
secreto aleatorio distinto del token. En desarrollo, abra un túnel HTTPS de
VS Code hacia el puerto HTTPS del backend y copie su URL pública sin `/` final.

```dotenv
Telegram__Enabled=true
Telegram__BotToken=<token entregado por BotFather>
Telegram__BotUsername=<nombre del bot sin @>
Telegram__WebhookSecret=secreto-aleatorio-con-letras-numeros-guion-o-guion-bajo
Telegram__PublicWebhookUrl=https://<url-publica-del-tunel>
Telegram__LinkCodeTtlMinutes=10
Telegram__WorkerPollMilliseconds=1000
Telegram__MaxProcessingAttempts=3
Telegram__DelegatedTokenMinutes=5
```

También deben estar configurados Oracle, JWT y `Agent__Enabled=true`. Aplique
la migración únicamente contra la base local confirmada:

```powershell
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

## 2. Registrar el webhook

Estos comandos leen los secretos desde variables de proceso y no los imprimen.
Ejecute PowerShell en la misma sesión donde asignó los valores:

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

Compruebe el registro sin mostrar el token:

```powershell
$webhookInfo = Invoke-RestMethod `
  -Method Get `
  -Uri "https://api.telegram.org/bot$telegramBotToken/getWebhookInfo"
$webhookInfo.result | Select-Object url, pending_update_count, last_error_message
```

Si cambia la URL del túnel debe ejecutar `setWebhook` otra vez.

## 3. Vincular y probar

1. Inicie el chatbot, Oracle y el backend.
2. Inicie sesión en Swagger y autorice con el access token.
3. Ejecute `POST /api/integrations/telegram/link-codes`.
4. Abra el `deepLink` retornado o envíe `/start <code>` al bot.
5. Espere la confirmación de vinculación y envíe un mensaje de texto.
6. Verifique que el bot responda y que Oracle contenga el vínculo, la
   conversación y el participante.

El `update_id` de Telegram evita procesar dos veces el mismo webhook. Los
fallos temporales quedan pendientes para reintento y el texto técnico del
inbox se elimina cuando termina o agota sus intentos.

## 4. Diagnóstico rápido

- `401/403` en el webhook: revise que el secreto registrado coincida con
  `Telegram__WebhookSecret`.
- El webhook acumula pendientes: revise el estado del backend y la conexión a
  Oracle; el worker solo se registra cuando `Telegram__Enabled=true`.
- El bot confirma vínculo pero no responde: compruebe `Agent__Enabled`, la URL
  interna del agente y que el usuario vinculado siga activo.
- `getWebhookInfo.last_error_message` ayuda a detectar túneles cerrados o
  certificados inaccesibles.

No registre en logs ni comparta el token del bot, el secreto del webhook, JWT,
códigos de vinculación o textos de usuarios.
