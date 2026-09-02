# Registro seguro de clientes desde Telegram

## Objetivo

Permitir que una persona sin cuenta de Huellitas inicie su registro desde un chat privado de Telegram, verifique la propiedad de su correo mediante OTP y complete sus datos sensibles en una página HTTPS temporal. Al finalizar, el backend debe crear la identidad, la cuenta, las credenciales y el perfil de cliente, y vincular el chat en una sola operación consistente.

El flujo debe mantener el backend .NET como propietario de la identidad, las reglas de negocio y la persistencia. El agente Python no debe crear usuarios ni recibir contraseñas, identificaciones u OTP.

## Decisiones

- Telegram solamente inicia el registro y verifica el correo.
- Nombre completo, identificación, nombre de usuario y contraseña se reciben en una página segura.
- La página mínima será reemplazable por el frontend definitivo mediante configuración.
- El registro existente y el registro iniciado por Telegram reutilizarán una única operación de creación de cuentas de cliente.
- El registro desde Telegram no emitirá JWT ni refresh tokens innecesarios.
- `/vincular` se conserva para cuentas existentes.
- Si `/registrar` recibe el correo de una cuenta activa, después de comprobar el OTP vinculará esa cuenta en lugar de duplicarla.

## Arquitectura

### Security/Registration

Una operación reutilizable `RegisterClientAccount` será responsable de crear de forma atómica:

- `Users`
- `UserAccounts`
- `UserCredentials`
- `Clients`
- asignación del rol `Cliente`

El endpoint `/api/auth/register` continuará usando esta operación y posteriormente emitirá sus tokens. El flujo de Telegram utilizará la misma operación, pero sin emitir tokens de autenticación.

### Telegram/Registration

Un servicio específico procesará `/registrar`, correo, OTP y generación del enlace. Este servicio no duplicará las responsabilidades de `TelegramChatLinkingService`, que seguirá atendiendo `/vincular`, `/desvincular` y sus sesiones existentes.

La entidad `TelegramRegistrationSession` representará el proceso mediante los estados:

```text
AwaitingEmail -> AwaitingOtp -> AwaitingProfile -> Completed
                       |             |
                       +--> Blocked  +--> Expired
                       +--> Expired  +--> Cancelled
```

La sesión conservará los identificadores de Telegram, hashes, intentos, vencimientos y el correo verificado de forma protegida. No almacenará contraseñas ni el OTP en texto plano.

### API temporal de registro

La API servirá una página mínima mediante:

```text
GET  /telegram/registration/complete?token=...
GET  /telegram/registration/complete
POST /telegram/registration/complete
```

El primer `GET` intercambiará el token por una cookie temporal `HttpOnly`, `Secure` y `SameSite=Strict`, y redirigirá hacia una URL sin el token. El formulario recibirá:

- nombre completo;
- número de identificación;
- nombre de usuario;
- contraseña;
- confirmación de contraseña.

El correo procederá exclusivamente de la verificación OTP anterior y no será editable desde el formulario.

## Flujo principal

1. Una persona no vinculada envía `/registrar` en un chat privado.
2. El backend abre una sesión y solicita el correo.
3. El usuario escribe el correo y el backend envía un OTP.
4. La respuesta no revela inicialmente si el correo ya existe.
5. El usuario escribe el OTP y el backend lo verifica.
6. Si existe una cuenta activa para el correo, el backend vincula el chat y completa el proceso.
7. Si no existe una cuenta, el backend emite un token aleatorio, de un solo uso y corta duración, y envía el enlace HTTPS.
8. El usuario abre la página y completa sus datos.
9. El backend valida la sesión, las unicidades y la contraseña.
10. En una transacción crea la cuenta completa, crea la vinculación de Telegram y consume la sesión.
11. La página confirma el resultado y permite volver a Telegram.
12. El chat puede utilizar inmediatamente las operaciones privadas, incluida la consulta y posterior creación de mascotas.

## Reglas de seguridad

- Solo se admite `/registrar` en chats privados.
- Un usuario ya vinculado debe ejecutar `/desvincular confirmar` antes de registrar otra cuenta.
- Solo puede existir una sesión activa por usuario de Telegram.
- OTP con tiempo de vida, intervalo de reenvío y máximo de intentos.
- Token final aleatorio, de un solo uso, con expiración y persistido solamente como hash.
- Correo verificado almacenado cifrado o protegido y acompañado por un hash para búsquedas seguras.
- Los mensajes que contienen correo u OTP se redactan después de procesarlos.
- Contraseña nunca enviada a Telegram, nunca registrada en logs y almacenada únicamente mediante el hasher existente.
- Identificación, OTP, correo y tokens quedan excluidos de logs y métricas.
- Rate limiting para OTP, intercambio del token y envío del formulario.
- HTTPS obligatorio salvo en desarrollo local controlado.
- El formulario no persiste datos en almacenamiento del navegador ni devuelve JWT.

## Consistencia e idempotencia

- La creación de `Users`, `UserAccounts`, `UserCredentials`, `Clients`, `TelegramUserLink` y el consumo de la sesión ocurre en una única transacción de base de datos.
- Un token utilizado no puede volver a consumirse.
- Una repetición del `POST` no crea una segunda cuenta.
- Un fallo antes del commit no deja cuentas ni vínculos parciales.
- Un fallo posterior al commit al enviar la confirmación por Telegram no revierte la cuenta; la página muestra el resultado confirmado.

## Errores y experiencia conversacional

- Correo ya registrado: después de verificar OTP, vincular la cuenta existente.
- Cuenta existente inactiva: no vincular ni duplicar; indicar que se requiere soporte.
- Usuario o identificación ocupados: mantener la sesión vigente y permitir corregir el formulario.
- OTP incorrecto: informar que no es válido y controlar intentos sin revelar datos de cuenta.
- OTP bloqueado o vencido: solicitar reiniciar con `/registrar`.
- Token vencido, consumido o inválido: no mostrar el formulario y solicitar reiniciar.
- Sesión cancelada: no permitir completar el formulario.
- Fallo interno: respuesta genérica al usuario y código técnico seguro en logs.

## Configuración

Configuración inicial propuesta:

```env
Telegram__RegistrationEnabled=true
Telegram__RegistrationCompletionUrl=https://dominio/telegram/registration/complete
Telegram__RegistrationOtpTtlMinutes=10
Telegram__RegistrationTokenTtlMinutes=15
Telegram__RegistrationMaxOtpAttempts=3
Telegram__RegistrationResendSeconds=60
```

`Telegram__RegistrationCompletionUrl` permitirá migrar al frontend definitivo sin cambiar la lógica de dominio ni el contrato de finalización.

## Pruebas dirigidas

Se ejecutará un conjunto acotado, no la suite completa, que cubra:

- transiciones principales de `TelegramRegistrationSession`;
- OTP inválido, vencido y bloqueado;
- token inválido, vencido, consumido y reutilizado;
- creación completa de cuenta y perfil `Client`;
- correo existente que termina en vinculación;
- conflictos de usuario e identificación;
- rollback ante un fallo de creación o vinculación;
- ausencia de contraseña, OTP, identificación y token en logs;
- compatibilidad del endpoint `/api/auth/register` existente.

## Fuera de alcance

- Diseño visual definitivo del frontend.
- Registro de mascotas dentro del mismo formulario.
- Inicio de sesión web desde Telegram.
- Proveedores de identidad externos.
- Recuperación completa de contraseña.
- Uso del LLM para decidir o ejecutar pasos del registro.
