# Smoke — puerta de kickoff Etapa 5 (docs / contratos)

## Objetivo

Verificar que el kickoff de Etapa 5 quedó **solo** como decisiones escritas, enlazadas, sin implementar los slices 5.1–5.5 en el mismo cambio.

## Checklist

| ID | Criterio | Evidencia |
|----|----------|-----------|
| K1 | ADR mergeado | `docs/adr/2026-09-07-etapa-5-client-portal-retirement.md` |
| K2 | Enlace desde CONTEXT | `docs/CONTEXT_REVISION_BACKEND.md` §0 → Etapa 5 |
| K3 | Lista ClientOnly copiable | ADR Anexos A–B |
| K4 | OTP citas = anónimo | ADR §3; rutas `request-code` / `confirm-code` |
| K5 | WhatsApp fuera de alcance | ADR §5; ningún sender/DTO WhatsApp nuevo en el kickoff |
| K6 | Convención 410 | `ClientPortal.Gone` + `application/problem+json` |
| K7 | 5.2 sin pelear controllers | ADR Anexo B (5.2a–e por archivo) |

## Fuera de este kickoff

Implementar 5.1 (teléfono), 5.2a–e (410/eliminar), 5.3 (seed), 5.4 (handler Gone), 5.5 (quitar policy). Cambiar Etapa 3/4.

## Comando

No hay suite de tests de kickoff: es gate documental. Revisión humana del checklist arriba.
