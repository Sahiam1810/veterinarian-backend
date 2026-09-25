# Smoke — puerta de kickoff Etapa 6 (docs / contratos)

## Objetivo

Verificar que el kickoff de Etapa 6 quedó **solo** como decisiones escritas, sin implementar 6.1–6.5 en el mismo cambio.

**Canal = Telegram. WhatsApp fuera de alcance.**

## Checklist kickoff

| ID | Criterio | Evidencia |
|----|----------|-----------|
| K1 | ADR mergeado | `docs/adr/2026-09-07-etapa-6-rate-limit-logging-codes-foundations.md` |
| K2 | Enlace CONTEXT | `docs/CONTEXT_REVISION_BACKEND.md` §0 → Etapa 6 |
| K3 | Anexo rutas rate limit | ADR Anexo A |
| K4 | Convención logs (sin PII/OTP/proof) | ADR §2 |
| K5 | Catálogo codes + punteros `*Errors.cs` | `docs/contracts/api-error-codes-catalog.md` |
| K6 | Dueños de archivo RL/LOG/CODE/CTX/SMOKE | ADR §5 |
| K7 | Cero código de producto en el PR de kickoff | diff solo `docs/` |

## Humo después de seed (usuarios reales del repo)

Ejecutar **después** de migraciones + seeds documentados. Usar credenciales del seed del proyecto — **no** placeholders de tutorial (`miusuario`, `user@example.com` inventado, `nombretabla`).

Referencias:

- `database/seeds/README.md` / `apply_all.sql`
- `database/test_seeds/README.md` (p. ej. Administrador `admin` / `admin@veterinaria.com` si ese seed está aplicado en el entorno)
- `docs/SUPERADMIN_PROVISIONING.md` (promoción SuperAdmin sobre cuenta real)

| Paso | Acción | Esperado |
|------|--------|----------|
| H1 | Login staff con usuario del seed | 200 + tokens; rate limit Login activo |
| H2 | Refresh con refresh token válido | 200; policy Refresh |
| H3 | Lookup anónimo by-phone / by-identification (dato del seed o fixture conocido) | 200 o 404 de negocio; 429 si se satura |
| H4 | Webhook Telegram con secreto inválido | rechazo; sin filtrar PII en logs |
| H5 | OTP cita request-code (teléfono de una cita real de prueba) | 202/4xx de negocio; no Gmail; rate limit |
| H6 | Revisar logs de H1–H5 | Sin teléfono, cédula, OTP, proof ni cuerpo de correo en claro |

## Fuera de este kickoff

Implementar 6.1–6.5. WhatsApp. Reabrir portal Cliente. OTP en UI staff.

## Comando

Gate documental: revisión humana del checklist K*.

Humo de **cierre de producto** (post-seed + filtros de test): ver [`etapa-6-objetivo-exit-gate.md`](etapa-6-objetivo-exit-gate.md) (tarea 6.4).
