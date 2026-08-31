using System.ComponentModel.DataAnnotations;
using Domain.Modules.ValueObjects;

namespace Api.Modules.Dtos;

public sealed record CreateModuleDto(
    [Required(ErrorMessage = "El nombre del módulo es obligatorio.")]
    [MaxLength(ModuleName.MaxLength, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name,
    string? Description);

public sealed record UpdateModuleDto(
    [Required(ErrorMessage = "El nombre del módulo es obligatorio.")]
    [MaxLength(ModuleName.MaxLength, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name,
    string? Description);

public sealed record ModuleResponseDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
