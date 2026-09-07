# ADR: RegisterOwner (Etapa 4)

- Estado: Aceptada
- Fecha: 2026-09-07
- Decisores: Equipo de desarrollo

## Contexto

Etapa 3 dejó proof Email de un solo uso (`ConsumeProof`) sin crear dueño. Staff, bot y Telegram necesitan el mismo núcleo transaccional (User Cliente + Client) para no duplicar handlers. El registro Telegram vigente (sesión/OTP propio) no debe seguir siendo un segundo alta de dueño: **Telegram termina en RegisterOwner**. Cliente no tiene password ni cuenta de plataforma (Etapa 1). WhatsApp y OTP SMS de contacto siguen fuera de alcance.

## Decisión

1. **Un comando Application: `RegisterOwner`.** Crea User rol Cliente **sin** `PASSWORD_HASH` y el perfil Client. No crea `USER_ACCOUNTS` ni `USER_CREDENTIALS`. El login de ese correo sigue denegado (sin account; aunque hubiera credenciales legacy, Etapa 1 responde `Authentication.PlatformAccessDenied`).
2. **ConsumeProof Email (Bot).** El canal Bot siempre consume un proof `Purpose=Register` cuyo hash de destino coincide con el correo. Un proof `Claim` no registra dueño nuevo.
3. **Equivalencia Telegram (tarea 4.3) — una sola regla.** El canal Telegram **no** consume `ContactVerification` ConsumeProof. La equivalencia escrita es: sesión `TelegramRegistrationSession` con OTP Gmail ya verificado (`AwaitingProfile` + completion token vigente + `ProtectedEmail`). Eso demuestra control del buzón antes de `RegisterOwner`. El formulario Complete no pide email ni password; el correo sale de la sesión. No hay dual path viejo+nuevo ni proof “opcional” en la vista.
4. **Flag `RegisterOwner:RequireContactProofs`.** Aplica **solo a staff**. Default `false`: el escritorio verifica identidad en persona. `true` obliga el mismo proof Email. Bot ignora el flag (siempre proof). Telegram ignora el flag (siempre equivalencia de sesión).
5. **Puertos en archivos distintos.** Staff, bot, Telegram y cleanup (barrido de sesiones vencidas) no comparten un controller gigante.
6. **Sin password opcional, sin WhatsApp, sin OTP SMS de contacto.** El canal de RegisterOwner es Staff | Bot | Telegram.

## Alternativas descartadas

- Tres handlers de alta (staff/bot/Telegram): se pisan y divergen reglas.
- Reusar CreateUser+CreateClient por separado: CreateUser staff exige password; CreateClient asume User ya existente; no consume proof.
- Staff siempre con proof: fricción en recepción; el flag lo habilita si el negocio lo pide.
- Forzar ContactVerification dentro del chat Telegram además del OTP propio: doble OTP al mismo buzón sin ganancia de producto.

## Consecuencias

- 4.1–4.5 solo adaptan HTTP/Telegram/cleanup al comando.
- Borrar el alta Telegram legacy (password/credentials) es la tarea 4.3.
- Conflictos email/cédula/teléfono se resuelven en el núcleo antes de persistir.

## Criterios de revisión

- [ ] ¿Crea account, credentials o `PASSWORD_HASH` para Cliente?
- [ ] ¿Permite login web de ese email?
- [ ] ¿Bot registra sin ConsumeProof Email?
- [ ] ¿Telegram registra sin OTP de sesión verificado (equivalencia) y sin password?
- [ ] ¿Declara WhatsApp o OTP SMS de contacto?
- [ ] ¿Staff y bot editan el mismo handler/controller?
