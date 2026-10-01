using Api.Supplies.Dtos;
using Application.Supplies.UseCases;
using Domain.Supplies.Entities;

namespace Api.Supplies.Mappings;

public static class SupplyMappings
{
    public static SupplyDto ToResponse(this Supply supply)
    {
        return new SupplyDto(
            supply.Id,
            supply.Name,
            supply.Unit,
            supply.UnitPrice,
            supply.Stock,
            supply.IsActive,
            supply.CreatedAt,
            supply.UpdatedAt);
    }

    public static IEnumerable<SupplyDto> ToResponse(this IEnumerable<Supply> supplies)
    {
        return supplies.Select(s => s.ToResponse());
    }

    public static CreateSupplyCommand ToCommand(this CreateSupplyDto dto)
    {
        return new CreateSupplyCommand(dto.Name, dto.Unit, dto.UnitPrice, dto.Stock, dto.IsActive);
    }

    public static UpdateSupplyCommand ToCommand(this UpdateSupplyDto dto, Guid id)
    {
        return new UpdateSupplyCommand(id, dto.Name, dto.Unit, dto.UnitPrice, dto.Stock, dto.IsActive);
    }
}
