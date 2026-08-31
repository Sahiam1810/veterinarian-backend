using System.ComponentModel.DataAnnotations;

namespace Api.Modules.Dtos;

public sealed record CreateModuleDto(
    [Required(ErrorMessage = "El nombre del módulo es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name,
    [MaxLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres.")]
    string? Description);

public sealed record UpdateModuleDto(
    [Required(ErrorMessage = "El nombre del módulo es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name,
    [MaxLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres.")]
    string? Description);

public sealed record ModuleResponseDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
