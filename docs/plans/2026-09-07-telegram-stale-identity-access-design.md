# Recuperación de identidad obsoleta en Telegram

## Problema

Cuando un chat conserva una fila activa en `TELEGRAM_USER_LINKS`, pero la persona vinculada ya no
resuelve a un cliente, usuario y cuenta activos, `TelegramIdentityAccessService` responde que el
perfil no está disponible. El flujo termina antes de solicitar cédula y no permite recuperar el
acceso ni registrar un cliente nuevo.

## Decisión

Una vinculación que no resuelve a una identidad de cliente activa se considera obsoleta. El
backend debe revocarla, conservar su trazabilidad y continuar la misma solicitud privada como un
acceso sin identidad conocida:

1. Capturar de forma protegida el mensaje privado pendiente.
2. Revocar la vinculación obsoleta mediante `TelegramUserLink.Revoke`.
3. Persistir una `TelegramIdentitySession` en estado `AwaitingIdentification`.
4. Solicitar la cédula al usuario.
5. Si la cédula corresponde a un cliente con usuario y cuenta activos, enviar el OTP al correo
   registrado sin exigir que su rol sea `Cliente`. La autorización posterior conserva el rol real
   y los permisos del JWT.
6. Si la cédula no existe, recopilar confirmación, nombre completo y correo; enviar OTP y crear el
   usuario, la cuenta y el perfil de cliente dentro de la transacción existente.
7. Al verificar el OTP, crear o reactivar el enlace y reanudar automáticamente el mensaje original.

## Compatibilidad

- El acceso con una vinculación válida conserva el envío directo de OTP.
- El acceso sin vinculación conserva el flujo actual por cédula.
- `/vincular` y `/registrar` no forman parte del flujo nuevo ni se muestran al usuario.
- Los componentes heredados de vinculación permanecen sin cambios para evitar una eliminación
  incompatible en esta corrección.
- `Administrador` y `SuperAdmin` pueden usar mascotas y citas cuando también tienen un perfil en
  `CLIENTS`; no se cambia su rol.
- Un conflicto residual de correo o cédula durante un registro se convierte en una respuesta
  controlada y cancela esa sesión, en vez de agotar reintentos del worker.
- No cambia ningún endpoint, contrato HTTP, tabla ni migración.

## Capas afectadas

- Domain: sin cambios; se reutiliza `TelegramUserLink.Revoke`.
- Application: cambia `TelegramIdentityAccessService.BeginPrivateAccessAsync`, agrega un error
  específico para conflictos de registro y lo maneja en `ProcessOtpAsync`.
- Infrastructure: `TelegramClientIdentityGateway` deja de confundir rol con identidad y emite el
  error específico cuando los datos de registro ya existen.
- Api: sin cambios; Telegram continúa entrando por el webhook actual.

## Errores y consistencia

La revocación del enlace, la creación de la sesión y el guardado deben completarse antes de
responder que se requiere la cédula. Los errores de persistencia conservan el mecanismo actual de
reintentos del procesamiento de Telegram. No se registra en logs la cédula, el OTP, el mensaje ni
el correo.

## Verificación

Las pruebas cubrirán una vinculación obsoleta, un cliente activo con rol administrativo y un
conflicto residual después de validar el OTP. Deben comprobar la solicitud de cédula, la resolución
de identidad sin cambiar roles y una respuesta controlada sin reintentos. También se ejecutarán las
pruebas enfocadas de identidad de Telegram y una compilación sin restore.
