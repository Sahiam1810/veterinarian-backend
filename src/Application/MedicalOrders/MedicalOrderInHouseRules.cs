using Application.Common.Exceptions;

namespace Application.MedicalOrders;

// Reglas compartidas: interna = ítems y sin remisión; remitida = remisión y cero ítems
public static class MedicalOrderInHouseRules
{
    public static void EnsureValid(
        bool isInHouse,
        int itemCount,
        string? referredTo,
        string? referralReason)
    {
        var hasReferral =
            !string.IsNullOrWhiteSpace(referredTo) &&
            !string.IsNullOrWhiteSpace(referralReason);

        if (isInHouse)
        {
            if (itemCount < 1)
            {
                throw new BadRequestException("Una orden interna requiere al menos un ítem del catálogo.");
            }

            if (!string.IsNullOrWhiteSpace(referredTo) || !string.IsNullOrWhiteSpace(referralReason))
            {
                throw new BadRequestException("Una orden interna no puede tener destino ni motivo de remisión.");
            }

            return;
        }

        if (!hasReferral)
        {
            throw new BadRequestException("Una orden remitida requiere destino y motivo de remisión.");
        }

        if (itemCount > 0)
        {
            throw new BadRequestException("Una orden remitida no puede incluir ítems del catálogo.");
        }
    }
}
