# ADR: Verificación de contacto por correo (Etapa 3)

- Estado: Aceptada
- Fecha: 2026-09-07
- Decisores: Equipo de desarrollo

## Contexto

Las etapas 0–2 cerraron identidad de Cliente (sin password) y OTP de acción de cita en tabla propia. El registro/reclamo de dueño necesita verificar un canal de contacto antes de crear perfil o vincular uno existente. Reutilizar `APPOINTMENT_ACTION_VERIFICATION_SESSIONS` o el OTP de Telegram mezclaría propósitos y permitiría usar un código fuera de su alcance.

WhatsApp/SMS de contacto no están en alcance de esta etapa. Un diseño que ya declare esos canales forzaría contratos que 3.1–3.4 no van a implementar.

## Decisión

1. **Propósitos explícitos: Register y Claim.** Register verifica correo para un dueño nuevo. Claim verifica correo para un cliente ya persistido. Ninguno autoriza cancelar/reagendar citas ni login por contraseña.
2. **Canal v1: solo Email.** La sesión de contacto no modela WhatsApp ni SMS. Esos canales quedan fuera de alcance hasta un ADR posterior.
3. **Tabla propia.** `CONTACT_VERIFICATION_SESSIONS` es independiente de OTP de citas y de sesiones Telegram. Prohibido persistir OTP o proof en claro; solo hashes.
4. **Proof de un solo uso.** Confirmar el OTP emite un proof (hash persistido, valor en claro solo en la respuesta de 3.2). `ConsumeProof` lo marca Consumed y borra el hash. Un segundo consume falla.
5. **Sesión activa.** Como máximo una sesión viva (`AwaitingOtp` o `ProofIssued`) por par propósito + hash de correo. Un reenvío dentro de la ventana de resend se rechaza; fuera de ella se cancela la anterior y se crea otra. Expirar o bloquear libera el cupo.
6. **Puertos separados.** Request Email, Confirm y ConsumeProof viven en archivos distintos para que 3.1–3.4 no se pisen. Esta apertura no implementa RegisterOwner.

## Alternativas descartadas

- Reusar OTP de citas: propósito y sujeto distintos (cita vs correo).
- Canal WhatsApp/SMS en v1: fuera de alcance; contaminaría DTOs y options.
- OTP reutilizable o proof sin consumir: viola un solo uso y el ADR de límites OTP.

## Consecuencias

- 3.1–3.4 implementan envío, confirmación, consume y registro sobre estos contratos.
- El front traduce `ContactVerification.*`; no hay UX hardcodeada en Description.
- Un OTP de contacto no autoriza acciones de cita; un OTP de cita no verifica correo.

## Criterios de revisión

- [ ] ¿Usa `APPOINTMENT_ACTION_VERIFICATION_SESSIONS` o guarda OTP/proof en claro?
- [ ] ¿Declara WhatsApp/SMS como canal de contacto v1?
- [ ] ¿El purpose no es Register ni Claim?
- [ ] ¿El proof se puede reutilizar después de ConsumeProof?
- [ ] ¿Hay más de una sesión viva por propósito + correo?
