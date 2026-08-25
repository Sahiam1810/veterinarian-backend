using Api.Diagnostics.Dtos;
using Domain.Diagnostics.Entities;

namespace Api.Diagnostics.Mappings;

public static class DiagnosticMapping
{
    public static DiagnosticDto ToDto(Diagnostic entity) =>
        new(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Description,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt
        );

    public static IEnumerable<DiagnosticDto> ToDtoList(IEnumerable<Diagnostic> entities) =>
        entities.Select(ToDto);
}
