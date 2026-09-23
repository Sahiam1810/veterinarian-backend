using Api.SupplyConsumptions.Dtos;
using Application.SupplyConsumptions.UseCases;
using Domain.SupplyConsumptions.Entities;

namespace Api.SupplyConsumptions.Mappings;

public static class SupplyConsumptionMappings
{
    public static SupplyConsumptionDto ToResponse(this SupplyConsumption consumption)
    {
        return new SupplyConsumptionDto(
            consumption.Id,
            consumption.HospitalizationStayId,
            consumption.SupplyId,
            consumption.Quantity,
            consumption.UnitPrice,
            consumption.Total,
            consumption.RegisteredByUserId,
            consumption.Notes,
            consumption.CreatedAt);
    }

    public static IEnumerable<SupplyConsumptionDto> ToResponse(this IEnumerable<SupplyConsumption> consumptions)
    {
        return consumptions.Select(c => c.ToResponse());
    }

    public static RegisterSupplyConsumptionCommand ToCommand(
        this CreateSupplyConsumptionDto dto,
        Guid stayId,
        Guid registeredByUserId)
    {
        return new RegisterSupplyConsumptionCommand(
            stayId,
            dto.SupplyId,
            dto.Quantity,
            registeredByUserId,
            dto.Notes);
    }
}
