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
Telegram__GuestModeEnabled=true
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
Telegram__PrivateAccessAbsoluteTtlHours=24
Telegram__PrivateAccessIdleTtlMinutes=30
Telegram__RegistrationProtectionKeyBase64=<32 bytes aleatorios codificados en Base64>

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

### Opción recomendada para pruebas locales: Cloudflare Quick Tunnel

Desde la raíz del backend, ejecute el siguiente comando en una terminal y déjela
abierta durante toda la prueba:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\start-telegram-cloudflare-tunnel.ps1
```

El script no inicia el backend. Comprueba `cloudflared` y los secretos existentes,
crea una URL temporal `trycloudflare.com`, actualiza únicamente
`Telegram__PublicWebhookUrl` en el `.env` privado y registra el webhook. Después de
ver la confirmación, abra otra terminal e inicie manualmente la API:

```powershell
dotnet run --project src/Api/Api.csproj --launch-profile http
```

El túnel apunta de forma predeterminada a `http://localhost:5233`. Si el backend usa
otro puerto, páselo explícitamente:

```powershell
.\scripts\start-telegram-cloudflare-tunnel.ps1 `
  -BackendUrl http://localhost:PUERTO
```

Cada ejecución genera una URL nueva y vuelve a registrar Telegram. Use `Ctrl+C` en
la terminal del script para detener el túnel.

### Opción manual

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

## 3. Verificación condicional mediante cédula y OTP

Con `Telegram__GuestModeEnabled=true`, cualquier chat privado puede hacer
preguntas veterinarias generales. El backend usa una identidad técnica
`TelegramGuest`; no crea conversaciones ni participantes para esa identidad y
el agente no permite que ejecute módulos privados.

La verificación comienza automáticamente, sin `/vincular`, cuando el agente
indica que la consulta requiere datos u operaciones privadas:

1. El backend conserva cifrada la consulta pendiente y solicita la cédula.
2. Si existe un cliente activo, envía un OTP al correo registrado.
3. Si no existe, solicita confirmación, nombre y correo, y envía el OTP a ese
   correo para crear un perfil de cliente sin contraseña.
4. Al validar el OTP, enlaza permanentemente el chat con la persona y reanuda
   una sola vez la consulta original.

La cédula, el nombre, el correo y la consulta pendiente se cifran con
`Telegram__RegistrationProtectionKeyBase64`; el OTP solo se guarda como hash.
Los mensajes entrantes que contienen datos sensibles se redactan del inbox. El
agente Python nunca recibe cédula, correo ni OTP.

El enlace del chat es persistente, pero la autorización privada es temporal.
Dura como máximo `Telegram__PrivateAccessAbsoluteTtlHours` y se invalida tras
`Telegram__PrivateAccessIdleTtlMinutes` sin actividad. Durante una sesión
vigente no vuelve a pedir OTP. Al vencer, las preguntas generales siguen
funcionando y solo una nueva solicitud privada activa otra verificación.

Use `/cancelar` para abandonar un flujo activo. Para liberar el enlace envíe
`/desvincular confirmar`. Los comandos antiguos `/vincular` y `/registrar` ya
no inician procesos distintos; el bot explica que la verificación es automática.

`Telegram__DelegatedTokenMinutes` controla únicamente el JWT interno que .NET
genera para llamar al agente; no representa la duración de la sesión privada.

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
