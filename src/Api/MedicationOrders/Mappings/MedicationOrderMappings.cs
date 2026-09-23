using Api.MedicationOrders.Dtos;
using Application.MedicationOrders.UseCases;
using Domain.MedicationOrders.Entities;

namespace Api.MedicationOrders.Mappings;

public static class MedicationOrderMappings
{
    public static MedicationOrderItemDto ToResponse(this MedicationOrderItem item)
    {
        return new MedicationOrderItemDto(
            item.Id,
            item.MedicationOrderId,
            item.MedicationId,
            item.Medication?.Name,
            item.Notes);
    }

    public static MedicationOrderDto ToResponse(this MedicationOrder order)
    {
        return new MedicationOrderDto(
            order.Id,
            order.ClientPetId,
            order.VeterinarianId,
            order.AppointmentId,
            order.IsInHouse,
            order.ReferredTo,
            order.ReferralReason,
            order.Status,
            order.CreatedAt,
            order.UpdatedAt,
            order.Items.Select(i => i.ToResponse()).ToList());
    }

    public static IEnumerable<MedicationOrderDto> ToResponse(this IEnumerable<MedicationOrder> orders)
    {
        return orders.Select(o => o.ToResponse());
    }

    public static MedicationOrderItemDto ToDto(this MedicationOrderItem item)
    {
        return item.ToResponse();
    }

    public static CreateMedicationOrderCommand ToCommand(this CreateMedicationOrderDto dto)
    {
        var items = dto.Items?.Select(i => new MedicationOrderItemInput(i.MedicationId, i.Notes)).ToList();
        return new CreateMedicationOrderCommand(
            dto.ClientPetId,
            dto.AppointmentId,
            dto.IsInHouse,
            dto.ReferredTo,
            dto.ReferralReason,
            items);
    }

    public static PendingMedicationOrderDto ToPendingDto(this MedicationOrder order)
    {
        return new PendingMedicationOrderDto(
            order.Id,
            order.ClientPet?.Pet?.Name?.Value ?? "Desconocida",
            order.ClientPet?.Client?.FullName?.Value ?? "Desconocido",
            order.AppointmentId,
            order.IsInHouse,
            order.Status,
            order.CreatedAt,
            order.Items.Select(i => i.ToDto()).ToList());
    }
}
