namespace Api.Procedures.Dtos;

public record ProcedureDto(
    Guid Id,
    string Name,
    string? Code,
    bool IsActive,
    decimal Price,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateProcedureDto(
    string Name,
    string? Code,
    bool IsActive = true,
    decimal Price = 0m);

public record UpdateProcedureDto(
    string Name,
    string? Code,
    bool IsActive,
    decimal Price = 0m);
