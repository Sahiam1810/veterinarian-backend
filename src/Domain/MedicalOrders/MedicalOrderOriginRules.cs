namespace Domain.MedicalOrders;

// Regla compartida: una orden médica nace de una cita o de una estancia de
// hospitalización, nunca de ambas ni de ninguna. Guid.Empty cuenta como ausente.
public static class MedicalOrderOriginRules
{
    public const string ExactlyOneOriginMessage =
        "Debe especificar exactamente un origen para la orden: AppointmentId o HospitalizationStayId.";

    public static bool HasValue(Guid? id) => id.HasValue && id.Value != Guid.Empty;

    public static bool HasExactlyOneOrigin(Guid? appointmentId, Guid? hospitalizationStayId) =>
        HasValue(appointmentId) ^ HasValue(hospitalizationStayId);

    public static (Guid? AppointmentId, Guid? HospitalizationStayId) Normalize(
        Guid? appointmentId,
        Guid? hospitalizationStayId)
    {
        if (!HasExactlyOneOrigin(appointmentId, hospitalizationStayId))
        {
            throw new ArgumentException(ExactlyOneOriginMessage);
        }

        return (
            HasValue(appointmentId) ? appointmentId : null,
            HasValue(hospitalizationStayId) ? hospitalizationStayId : null);
    }
}
