# Configuración del canal Telegram

Esta integración recibe texto de chats privados, vincula la identidad de
Telegram con una cuenta Huellitas y utiliza el mismo flujo del módulo `Agent`.
Persiste la conversación, el participante y el estado técnico del webhook,
pero todavía no guarda el historial en `CHAT_MESSAGES`.

## 1. Preparar la configuración

Desde `@BotFather`, cree el bot y copie el token solamente en `.env`. Genere un
secreto aleatorio distinto del token. En desarrollo, abra un túnel HTTPS hacia
el puerto HTTPS del backend y copie su URL pública sin `/` final.

```dotenv
Telegram__Enabled=true
Telegram__BotToken=<token entregado por BotFather>
Telegram__BotUsername=<nombre del bot sin @>
Telegram__WebhookSecret=secreto-aleatorio-con-letras-numeros-guion-o-guion-bajo
Telegram__PublicWebhookUrl=https://<url-publica-del-tunel>
Telegram__LinkCodeTtlMinutes=10
Telegram__WorkerPollMilliseconds=30000
Telegram__ProcessingLeaseSeconds=300
Telegram__MaxProcessingAttempts=3
Telegram__DelegatedTokenMinutes=5
Telegram__OtpTtlMinutes=5
Telegram__OtpMaximumAttempts=5
Telegram__OtpResendSeconds=60
Telegram__OtpPepperBase64=<32 bytes aleatorios codificados en Base64>

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
`Telegram__WorkerPollMilliseconds` es solamente el intervalo de respaldo para
recuperar trabajo si una señal local se pierde; aumentarlo a 30000 no agrega
30 segundos a la respuesta normal. El override de Serilog evita imprimir cada
consulta exitosa de EF Core y conserva visibles las advertencias y errores.

Genere el pepper una sola vez para el ambiente y consérvelo como secreto:

```powershell
$otpPepper = New-Object byte[] 32
$randomGenerator = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $randomGenerator.GetBytes($otpPepper) } finally { $randomGenerator.Dispose() }
[Convert]::ToBase64String($otpPepper)
```

No cambie el pepper mientras existan verificaciones pendientes. Para Gmail u
otro proveedor con autenticación multifactor utilice una clave de aplicación,
no la contraseña personal de la cuenta.

También deben estar configurados Oracle, JWT y `Agent__Enabled=true`. Aplique
las migraciones únicamente contra la base local confirmada:

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

## 3. Vincular desde Telegram mediante correo y OTP

1. Envíe `/vincular` en un chat privado con el bot.
2. Escriba el correo registrado en la cuenta activa de Huellitas.
3. Revise ese correo y escriba en Telegram el OTP recibido.
4. Espere la confirmación y envíe un mensaje veterinario normal.

El bot devuelve la misma respuesta cuando el correo no existe o está
inactivo. El OTP vence en cinco minutos, permite cinco intentos y no se guarda
en texto claro. Use `/cancelar` para abandonar una verificación. Para quitar
una vinculación activa, envíe `/desvincular` y después
`/desvincular confirmar`.

Desvincular conserva el historial, pero libera la persona, el usuario y el
chat de Telegram para completar una vinculación nueva. Un chat no vinculado no
envía mensajes al agente: cualquier saludo o `/start` sin código responde con
la instrucción de usar `/vincular`.

La vinculación es persistente: no vence cada cinco o quince minutos. La
variable `Telegram__DelegatedTokenMinutes` controla solamente el JWT interno
que el backend genera automáticamente para cada llamada al agente.

## 4. Vinculación alternativa desde la aplicación

1. Inicie el chatbot, Oracle y el backend.
2. Inicie sesión en Swagger y autorice con el access token.
3. Ejecute `POST /api/integrations/telegram/link-codes`.
4. Abra el `deepLink` retornado o envíe `/start <code>` al bot.
5. Espere la confirmación de vinculación y envíe un mensaje de texto.
6. Verifique que Oracle contenga el vínculo, la conversación y el participante.

El `update_id` evita procesar dos veces el mismo webhook. Los mensajes que
contienen correo u OTP se redactan del inbox durante su procesamiento y nunca
deben aparecer en logs.

## 5. Diagnóstico rápido

- `401/403` en el webhook: revise que el secreto registrado coincida con
  `Telegram__WebhookSecret`.
- El webhook acumula pendientes: revise el backend y Oracle; el worker solo se
  registra cuando `Telegram__Enabled=true`.
- El bot confirma vínculo pero no responde: compruebe `Agent__Enabled`, la URL
  interna del agente y que el usuario vinculado siga activo.
- El bot no puede enviar el OTP: revise `Email__Enabled`, host, puerto, TLS,
  credenciales SMTP y la política de claves de aplicación del proveedor.
- El backend no inicia por configuración OTP: confirme que
  `Telegram__OtpPepperBase64` decodifique al menos 32 bytes.
- `getWebhookInfo.last_error_message` ayuda a detectar túneles cerrados o
  certificados inaccesibles.

No registre en logs ni comparta el token del bot, el secreto del webhook, JWT,
OTP, credenciales SMTP, correos o textos de usuarios.
