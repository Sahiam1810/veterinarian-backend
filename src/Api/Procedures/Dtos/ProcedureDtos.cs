namespace Api.Procedures.Dtos;

public record ProcedureDto(
    Guid Id,
    string Name,
    string? Code,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateProcedureDto(
    string Name,
    string? Code,
    bool IsActive = true);

public record UpdateProcedureDto(
    string Name,
    string? Code,
    bool IsActive);
