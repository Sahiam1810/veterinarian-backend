namespace Api.Supplies.Dtos;

public record SupplyDto(
    Guid Id,
    string Name,
    string Unit,
    decimal UnitPrice,
    decimal Stock,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateSupplyDto(
    string Name,
    string Unit,
    decimal UnitPrice,
    decimal Stock,
    bool IsActive = true);

public record UpdateSupplyDto(
    string Name,
    string Unit,
    decimal UnitPrice,
    decimal Stock,
    bool IsActive);
