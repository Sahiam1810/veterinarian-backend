namespace Domain.MedicalOrders;

// Regla compartida: interna = ítems y sin remisión; remitida = remisión y cero ítems.
// Vive en Domain para que las entidades puedan invocarla sin depender de Application.
public static class MedicalOrderInHouseRules
{
    public static void Validate(
        bool isInHouse,
        string? referredTo,
        string? referralReason,
        int itemCount)
    {
        if (isInHouse)
        {
            if (itemCount < 1)
            {
                throw new ArgumentException("Una orden médica interna debe tener al menos 1 ítem.");
            }

            if (!string.IsNullOrWhiteSpace(referredTo) || !string.IsNullOrWhiteSpace(referralReason))
            {
                throw new ArgumentException("Una orden médica interna no puede contener datos de remisión.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(referredTo))
            {
                throw new ArgumentException("El lugar de remisión (ReferredTo) es obligatorio cuando la orden es externa.");
            }

            if (string.IsNullOrWhiteSpace(referralReason))
            {
                throw new ArgumentException("El motivo de remisión (ReferralReason) es obligatorio cuando la orden es externa.");
            }

            if (itemCount > 0)
            {
                throw new ArgumentException("Una orden médica remitida no debe contener ítems del catálogo interno.");
            }
        }
    }
}
