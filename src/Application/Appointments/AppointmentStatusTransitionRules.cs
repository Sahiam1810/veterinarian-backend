using Application.Common.Exceptions;

namespace Application.Appointments;

// Reglas de transición de estado, compartidas por el endpoint canónico
// (PATCH /api/appointments/{id}/status) y por la creación de historial vía
// AppointmentStatusHistoriesController -- antes este último no las aplicaba,
// dejando crear cualquier transición (incluso CANCELADA -> AGENDADA) sin
// ninguna restricción.
internal static class AppointmentStatusTransitionRules
{
    public const string Agendada = AppointmentStatusNames.Agendada;
    public const string Confirmada = AppointmentStatusNames.Confirmada;

    // AGENDADA: recepción puede marcar la llegada del paciente (check-in) sin
    // saltarse el flujo de atención, o cerrar la cita directamente.
    // CONFIRMADA: paciente ya en sala de espera; el veterinario la sigue
    // atendiendo normalmente, o recepción todavía puede cancelarla/marcar
    // inasistencia si el dueño se retira antes de la consulta.
    private static readonly Dictionary<string, HashSet<string>> AllowedTargetsByCurrentStatus =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Agendada] = new(StringComparer.OrdinalIgnoreCase)
            {
                "CONFIRMADA",
                "ATENDIDA",
                "CANCELADA",
                "NO_ASISTIO"
            },
            [Confirmada] = new(StringComparer.OrdinalIgnoreCase)
            {
                "ATENDIDA",
                "CANCELADA",
                "NO_ASISTIO"
            },
        };

    private static readonly HashSet<string> CommentRequiredTargets =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CANCELADA",
            "NO_ASISTIO"
        };

    public static void EnsureValidTransition(
        string currentStatusName,
        string targetStatusName,
        string? comment)
    {
        if (!AllowedTargetsByCurrentStatus.TryGetValue(currentStatusName, out var allowedTargets)
            || !allowedTargets.Contains(targetStatusName))
        {
            throw new ConflictException("La transición de estado solicitada no está permitida.");
        }

        if (CommentRequiredTargets.Contains(targetStatusName) && string.IsNullOrWhiteSpace(comment))
        {
            throw new BadRequestException("El comentario es requerido para este estado.");
        }
    }
}
