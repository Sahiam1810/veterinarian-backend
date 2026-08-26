using System.ComponentModel.DataAnnotations;

namespace Api.Specialties.Dtos;

public sealed record CreateSpecialtyDto(
    [Required(ErrorMessage = "El nombre de la especialidad es obligatorio.")]
    [MaxLength(30, ErrorMessage = "El nombre no puede superar los 30 caracteres.")]
    string Name,
    [MaxLength(30, ErrorMessage = "La descripción no puede superar los 30 caracteres.")]
    string? Description);

public sealed record UpdateSpecialtyDto(
    [Required(ErrorMessage = "El nombre de la especialidad es obligatorio.")]
    [MaxLength(30, ErrorMessage = "El nombre no puede superar los 30 caracteres.")]
    string Name,
    [MaxLength(30, ErrorMessage = "La descripción no puede superar los 30 caracteres.")]
    string? Description);

public sealed record SpecialtyResponseDto(Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime? UpdatedAt);
