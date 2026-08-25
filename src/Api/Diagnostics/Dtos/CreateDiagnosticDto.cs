namespace Api.Diagnostics.Dtos;

public record CreateDiagnosticDto(
    string Code,
    string Name,
    string? Description
);
