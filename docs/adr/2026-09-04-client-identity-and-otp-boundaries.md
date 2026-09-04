# ADR: Modelo de Cliente e independencia de OTP

- Estado: Aceptada
- Fecha: 2026-09-04
- Decisores: Equipo de desarrollo

## Contexto

El Cliente se modela como persona/usuario más un perfil de cliente asociado. Internamente pueden coexistir identificadores técnicos (usuario, cuenta), pero la decisión fija el teléfono como llave operativa objetivo de contacto e identificación frente a canales conversacionales y autoservicio.

Hoy no debe asumirse esa llave como ya implementada: `PHONE_NUMBER` puede ser opcional, no se ha confirmado índice único por teléfono, y el OTP de contacto/identidad vigente en Telegram verifica correo, no teléfono. Obligatoriedad, unicidad persistida y verificación telefónica por OTP requieren flujos y esquema explícitos.

Los flujos de Telegram, Accounts, onboarding y agenda deben evitar credenciales por contraseña para el Cliente. El dominio ya trata `PASSWORD_HASH` como opcional para ese rol y la creación de usuarios Cliente no admite contraseña; reabrir login, hash, reset o validación de password para Cliente es incompatible con la interfaz vía chatbot.

En paralelo existen OTP con propósitos distintos: verificación de contacto/identidad (vinculación o registro por canal) y autorización de una acción puntual sobre una cita (cancelar o reagendar). Mezclarlos permitiría usar un código fuera de su propósito —p. ej. un OTP de contacto para alterar una cita, o uno de cita para cambiar identidad o iniciar sesión—.

## Decisión

1. **Cliente = User + perfil Client.** Siempre hay usuario; el perfil de cliente es la extensión de dominio del dueño.
2. **User sí; password no para Cliente.** No crear ni revivir login por contraseña, `PASSWORD_HASH`, reset ni validación de password para el rol Cliente.
3. **Teléfono como llave operativa objetivo.** El teléfono es la llave de contacto/identificación a la que apuntan los flujos de Cliente. Puede aplicarse la validación vigente de normalización (solo dígitos, 7–20) donde ya exista; no sustituye identificadores internos. No se asume obligatoriedad, índice único por `PHONE_NUMBER` ni OTP telefónico ya implementados: deben garantizarse con esquema y flujos explícitos. Las unicidades del perfil (identificación única; un perfil por usuario) no equivalen a unicidad por teléfono.
4. **OTP con propósito, sujeto y expiración.** Cada OTP está ligado a un propósito explícito, a un sujeto/recurso y a una ventana de validez.
5. **Separación de dominios OTP.** El OTP de contacto/identidad no confirma, cancela ni reprograma citas. El OTP de acción de cita no verifica ni actualiza contacto, ni inicia sesión. Mientras la verificación vigente sea por correo u otro canal, ese OTP mantiene su propósito de contacto/identidad y no se convierte en OTP de acción de cita.
6. **Sin reutilización cruzada.** Un OTP no se reutiliza entre propósitos ni entre recursos distintos (contacto, identidad, cancelación, reagendado u otras acciones de cita).

## Límites de OTP

| Tipo | Propósito | Alcance | No autoriza |
|---|---|---|---|
| OTP de contacto/identidad | Verificar canal de contacto o identidad (p. ej. correo en vinculación/registro) | Usuario/cliente y canal de contacto | Confirmar, cancelar o reprogramar una cita |
| OTP de acción de cita | Autorizar una acción puntual sobre una cita (cancelar/reagendar) | Cita + acción + solicitante + expiración | Login, identidad general o cambio de teléfono |

## Consecuencias operativas

- La decisión fija el teléfono como llave operativa objetivo; no autoriza asumir obligatoriedad, unicidad persistida u OTP telefónico ya existentes.
- Mientras la verificación de contacto vigente sea por correo u otro canal, ese OTP sigue siendo de contacto/identidad y no autoriza acciones de cita.
- Todo flujo nuevo debe reutilizar abstracciones/propósitos OTP existentes o declarar un propósito aislado.
- Queda prohibido un OTP global reutilizable entre dominios.
- Un OTP de cita no puede modificar perfil ni contacto; la verificación de contacto no autoriza acciones de cita.
- El OTP no reemplaza autorización u ownership; toda ruta nueva debe validarlos además del OTP cuando aplique.
- Queda prohibido reintroducir password/`PASSWORD_HASH` para Cliente.

## Criterios de revisión

- [ ] ¿El cambio introduce password/`PASSWORD_HASH` para Cliente?
- [ ] ¿Trata el teléfono como llave operativa objetivo sin asumir obligatoriedad/unicidad/OTP telefónico ya implementados?
- [ ] ¿El OTP tiene propósito explícito y ligado al recurso/acción correcta?
- [ ] ¿El OTP se puede reutilizar fuera de propósito?
- [ ] ¿La autorización/ownership sigue separada de la verificación OTP?