using Api.ProcedureOrders.Dtos;
using Application.ProcedureOrders.UseCases;
using Domain.ProcedureOrders.Entities;

namespace Api.ProcedureOrders.Mappings;

public static class ProcedureOrderMappings
{
    public static ProcedureOrderItemDto ToResponse(this ProcedureOrderItem item)
    {
        return new ProcedureOrderItemDto(
            item.Id,
            item.ProcedureOrderId,
            item.ProcedureId,
            item.Procedure?.Name,
            item.Notes);
    }

    public static ProcedureOrderDto ToResponse(this ProcedureOrder order)
    {
        return new ProcedureOrderDto(
            order.Id,
            order.ClientPetId,
            order.VeterinarianId,
            order.AppointmentId,
            order.IsInHouse,
            order.ReferredTo,
            order.ReferralReason,
            order.Status,
            order.ResultFileUrl,
            order.CreatedAt,
            order.UpdatedAt,
            order.Items.Select(i => i.ToResponse()).ToList(),
            order.HospitalizationStayId);
    }

    public static IEnumerable<ProcedureOrderDto> ToResponse(this IEnumerable<ProcedureOrder> orders)
    {
        return orders.Select(o => o.ToResponse());
    }

    public static ProcedureOrderItemDto ToDto(this ProcedureOrderItem item)
    {
        return item.ToResponse();
    }

    public static CreateProcedureOrderCommand ToCommand(this CreateProcedureOrderDto dto, Guid actorUserId)
    {
        var items = dto.Items?.Select(i => new ProcedureOrderItemInput(i.ProcedureId, i.Notes)).ToList();
        return new CreateProcedureOrderCommand(
            dto.ClientPetId,
            dto.AppointmentId,
            dto.IsInHouse,
            dto.ReferredTo,
            dto.ReferralReason,
            items,
            dto.HospitalizationStayId,
            actorUserId);
    }

    public static PendingProcedureOrderDto ToPendingDto(this ProcedureOrder order)
    {
        return new PendingProcedureOrderDto(
            order.Id,
            order.ClientPet?.Pet?.Name?.Value ?? "Desconocida",
            order.ClientPet?.Client?.FullName?.Value ?? "Desconocido",
            order.AppointmentId,
            order.IsInHouse,
            order.Status,
            order.ResultFileUrl,
            order.CreatedAt,
            order.Items.Select(i => i.ToDto()).ToList(),
            order.HospitalizationStayId);
    }
}
