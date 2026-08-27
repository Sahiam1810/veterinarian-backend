using System.ComponentModel.DataAnnotations;

namespace Api.Priorities.Dtos;

public sealed record CreatePriorityDto(
    [Required(ErrorMessage = "El nombre de la prioridad es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);

public sealed record UpdatePriorityDto(
    [Required(ErrorMessage = "El nombre de la prioridad es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);

public sealed record PriorityResponseDto(Guid Id, string Name, DateTime CreatedAt, DateTime? UpdatedAt);
