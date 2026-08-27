using Api.Diagnostics.Dtos;
using Application.Diagnostics.UseCases;
using Domain.Diagnostics.Entities;

namespace Api.Diagnostics.Mappings;

public static class DiagnosticMapping
{
    public static CreateDiagnosticCommand ToCommand(
        this CreateDiagnosticDto dto)
    {
        return new CreateDiagnosticCommand(
            dto.Code,
            dto.Name,
            dto.Description);
    }

    public static UpdateDiagnosticCommand ToCommand(
        this UpdateDiagnosticDto dto,
        Guid id)
    {
        return new UpdateDiagnosticCommand(
            id,
            dto.Code,
            dto.Name,
            dto.Description,
            dto.IsActive);
    }

    public static DiagnosticDto ToResponse(this Diagnostic entity) =>
        new(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Description,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt
        );

    public static IReadOnlyCollection<DiagnosticDto> ToResponse(
        this IReadOnlyCollection<Diagnostic> entities) =>
        entities.Select(ToResponse).ToArray();
}
