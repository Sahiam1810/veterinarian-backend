# Vinculación de Telegram mediante correo y OTP

## Objetivo

Permitir que una persona con una cuenta activa de Huellitas vincule su usuario de Telegram desde el propio chat, sin compartir su contraseña y sin tener que autenticarse nuevamente cada pocos minutos.

La vinculación entre la cuenta de Huellitas y Telegram es persistente. La corta vigencia del JWT delegado se mantiene como un detalle interno: el backend emite uno nuevo para cada comunicación con el agente y el usuario nunca lo administra.

No forma parte de este alcance registrar cuentas nuevas desde Telegram.

## Decisiones principales

- La persona inicia el proceso con `/vincular`.
- La identidad se comprueba mediante un OTP enviado al correo de una cuenta existente.
- Telegram nunca solicita ni procesa la contraseña de Huellitas.
- El primer adaptador de entrega de OTP utiliza SMTP y se configura mediante variables de entorno.
- El módulo Telegram coordina el flujo, pero consulta usuarios y envía correos mediante contratos de aplicación.
- La vinculación definitiva continúa almacenándose en `TELEGRAM_USER_LINKS`.
- Las sesiones de verificación temporales sobreviven reinicios porque se almacenan en Oracle.

## Flujo funcional

1. Una persona no vinculada envía `/vincular`.
2. El bot solicita el correo registrado en Huellitas.
3. El siguiente mensaje se interpreta como correo solamente dentro de esa sesión.
4. El backend normaliza el correo y consulta una cuenta activa mediante un puerto del módulo de identidad.
5. La respuesta de Telegram es genérica, exista o no la cuenta, para evitar enumeración de usuarios.
6. Si la cuenta es válida, el backend genera un OTP aleatorio, guarda únicamente su hash y lo envía mediante el adaptador SMTP.
7. El siguiente mensaje se interpreta como OTP.
8. Si el OTP es válido, se crea `TelegramUserLink` y la sesión temporal se consume.
9. Los mensajes posteriores resuelven directamente la vinculación persistente y pasan al agente.
10. Para cada llamada al agente, el backend genera transparentemente un JWT delegado de corta duración.

`/cancelar` finaliza una verificación incompleta. `/desvincular` requiere confirmación y elimina o revoca la vinculación persistente.

## Estados de la sesión

```text
AwaitingEmail -> AwaitingOtp -> Linked
       |              |
       +----> Cancelled / Expired / Blocked
```

Solo puede existir una sesión activa por usuario de Telegram. Iniciar `/vincular` reemplaza de manera segura cualquier sesión incompleta anterior.

## Persistencia

Una entidad temporal, denominada conceptualmente `TelegramLinkingSession`, conserva:

- identificador del usuario de Telegram;
- identificador del chat privado;
- identificador interno de la persona una vez resuelto;
- hash del correo normalizado cuando sea necesario para controles de frecuencia;
- hash del OTP;
- estado de la sesión;
- fecha de expiración;
- cantidad de intentos;
- fecha desde la que puede solicitarse otro código;
- fechas de creación y actualización.

El correo en texto claro solo existe durante el procesamiento necesario para buscar la cuenta y enviar el mensaje. El OTP en texto claro solo existe durante su generación y entrega. Ninguno se guarda en la sesión.

## Límites modulares

### Telegram

- Mantiene la máquina de estados de vinculación.
- Interpreta `/vincular`, `/cancelar` y `/desvincular`.
- Persiste sesiones temporales y vinculaciones externas.
- No accede directamente a tablas de otros módulos.

### Identidad y cuentas

- Expone un contrato para resolver una cuenta activa por correo.
- Devuelve exclusivamente la identidad mínima necesaria.
- Conserva la propiedad de usuarios, cuentas y estados.

### Notificaciones

- Expone `IVerificationCodeSender` o un contrato equivalente.
- Infrastructure implementa inicialmente el contrato mediante SMTP.
- La elección futura de otro proveedor no modifica la lógica de Telegram.

### Agente

- No participa en el proceso OTP.
- Solo recibe mensajes cuando existe una vinculación válida.
- Continúa recibiendo un JWT delegado generado internamente por petición.

## Configuración SMTP

La configuración se obtiene del entorno, con nombres equivalentes a:

```env
Email__Enabled=true
Email__Host=smtp.example.com
Email__Port=587
Email__Username=
Email__Password=
Email__FromAddress=no-reply@huellitas.com
Email__FromName=Huellitas
Email__UseTls=true
```

La aplicación valida al arrancar la configuración requerida cuando el envío está habilitado. Ninguna credencial SMTP se expone mediante logs, errores, health checks o endpoints informativos.

## Seguridad

- OTP criptográficamente aleatorio y de un solo uso.
- Vigencia inicial recomendada: cinco minutos.
- Máximo inicial recomendado: cinco intentos.
- Comparación del hash en tiempo constante.
- Tiempo mínimo entre reenvíos y límites por usuario de Telegram, chat y correo normalizado.
- Respuestas indistinguibles para cuentas inexistentes, inactivas o no elegibles.
- Una cuenta ya vinculada a otro usuario de Telegram no se reemplaza automáticamente.
- El proceso solo funciona en chats privados.
- Correo, OTP, contraseñas, credenciales SMTP y JWT quedan fuera de logs.
- Los mensajes que contienen correo u OTP se eliminan o enmascaran en `TELEGRAM_INBOUND_UPDATES` tan pronto como se identifica su naturaleza sensible.

## Errores y recuperación

- Un fallo SMTP no consume la sesión; permite reintentar de forma controlada.
- Un OTP incorrecto incrementa los intentos sin revelar la causa exacta.
- Al agotar los intentos, la sesión queda bloqueada.
- Una sesión vencida obliga a iniciar nuevamente `/vincular`.
- `/cancelar` siempre tiene prioridad durante el flujo.
- Los mensajes normales se interpretan según el estado de la sesión.
- Los reinicios del proceso no pierden el estado pendiente.

## Observabilidad

Se registran eventos estructurados sin contenido sensible:

- sesión iniciada, cancelada, vencida, bloqueada o completada;
- entrega SMTP exitosa o fallida, sin destinatario completo ni OTP;
- latencia del proveedor SMTP;
- cantidad de solicitudes y verificaciones fallidas;
- vinculaciones y desvinculaciones;
- identificadores técnicos de correlación, nunca el texto sensible.

## Pruebas previstas

La verificación se limita a los escenarios de mayor riesgo:

- vinculación exitosa;
- OTP incorrecto, vencido y bloqueado;
- cuenta inexistente con respuesta indistinguible;
- conflicto con una vinculación existente;
- fallo recuperable del adaptador SMTP;
- continuidad de una sesión después de reiniciar;
- prioridad de `/cancelar`;
- ausencia de correo, OTP, credenciales y JWT en logs y persistencia no autorizada;
- mensajes sucesivos sin repetir autenticación después de vincularse;
- confirmación de que el JWT delegado se renueva internamente sin intervención del usuario.

## Fuera de alcance

- Registro de nuevas cuentas desde Telegram.
- Autenticación mediante contraseña dentro del chat.
- Soporte para grupos o canales.
- Proveedores de correo adicionales a SMTP.
- Guardar sesiones web o refresh tokens dentro del módulo Telegram.
