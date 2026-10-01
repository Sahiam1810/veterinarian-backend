namespace Api.SupplyConsumptions.Dtos;

public record SupplyConsumptionDto(
    Guid Id,
    Guid HospitalizationStayId,
    Guid SupplyId,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total,
    Guid RegisteredByUserId,
    string? Notes,
    DateTime CreatedAt);

public record CreateSupplyConsumptionDto(
    Guid SupplyId,
    decimal Quantity,
    string? Notes = null);

public record SupplyConsumptionTotalDto(
    Guid HospitalizationStayId,
    decimal Total);
