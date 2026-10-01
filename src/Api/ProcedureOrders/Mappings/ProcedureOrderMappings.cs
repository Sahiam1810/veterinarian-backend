using Api.ProcedureOrders.Dtos;
using Application.Common.Models;
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
            item.Notes,
            item.UnitPrice);
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

    public static ClinicalResultDto ToClinicalResultDto(this ProcedureOrder order)
    {
        var firstItem = order.Items.FirstOrDefault();

        return new ClinicalResultDto(
            order.Id,
            order.ClientPetId,
            order.AppointmentId,
            order.HospitalizationStayId,
            order.ClientPet?.Pet?.Name?.Value ?? "Desconocida",
            order.ClientPet?.Pet?.Species?.Name?.Value,
            order.ClientPet?.Pet?.Race?.Name?.Value,
            order.ClientPet?.Client?.FullName?.Value ?? "Desconocido",
            order.ClientPet?.Client?.PhoneNumber?.Value,
            firstItem?.Procedure?.Name,
            firstItem?.Notes,
            GetClinicalResultStatus(order),
            order.CreatedAt,
            order.UpdatedAt,
            order.VeterinarianId,
            order.Veterinarian?.User?.FullName,
            firstItem?.UnitPrice,
            order.ResultFileUrl ?? string.Empty,
            order.Items.Select(item => new ClinicalResultItemDto(
                item.Id,
                item.ProcedureId,
                item.Procedure?.Name,
                item.Notes,
                item.UnitPrice)).ToArray());
    }

    private static string GetClinicalResultStatus(ProcedureOrder order)
    {
        if (string.Equals(order.Status, ProcedureOrder.CompletedStatus, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(order.ResultFileUrl))
        {
            return "COMPLETED_WITH_RESULT";
        }

        if (string.Equals(order.Status, ProcedureOrder.CompletedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return "COMPLETED_WITHOUT_RESULT";
        }

        return "PENDING";
    }

    public static PaginatedClinicalResultResponse ToClinicalResultResponse(
        this PaginatedResult<ProcedureOrder> result)
    {
        return new PaginatedClinicalResultResponse(
            result.Items.Select(item => item.ToClinicalResultDto()).ToArray(),
            result.Pagination);
    }
}
