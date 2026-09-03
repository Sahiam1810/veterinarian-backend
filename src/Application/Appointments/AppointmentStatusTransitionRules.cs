using Application.Common.Exceptions;

namespace Application.Appointments;

// Reglas de transición de estado, compartidas por el endpoint canónico
// (PATCH /api/appointments/{id}/status) y por la creación de historial vía
// AppointmentStatusHistoriesController -- antes este último no las aplicaba,
// dejando crear cualquier transición (incluso CANCELADA -> AGENDADA) sin
// ninguna restricción.
internal static class AppointmentStatusTransitionRules
{
    public const string Agendada = "AGENDADA";

    private static readonly HashSet<string> AllowedTargetsFromAgendada =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ATENDIDA",
            "CANCELADA",
            "NO_ASISTIO"
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
        if (!string.Equals(currentStatusName, Agendada, StringComparison.OrdinalIgnoreCase)
            || !AllowedTargetsFromAgendada.Contains(targetStatusName))
        {
            throw new ConflictException("La transición de estado solicitada no está permitida.");
        }

        if (CommentRequiredTargets.Contains(targetStatusName) && string.IsNullOrWhiteSpace(comment))
        {
            throw new BadRequestException("El comentario es requerido para este estado.");
        }
    }
}
