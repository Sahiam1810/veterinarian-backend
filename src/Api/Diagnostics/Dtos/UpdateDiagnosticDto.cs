namespace Api.Diagnostics.Dtos;

public record UpdateDiagnosticDto(
    string Code,
    string Name,
    string? Description,
    bool IsActive
);
