using System.ComponentModel.DataAnnotations;

namespace Api.EscalationStatuses.Dtos;

public sealed record CreateEscalationStatusDto(
    [Required(ErrorMessage = "El nombre del estado de escalamiento es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);

public sealed record UpdateEscalationStatusDto(
    [Required(ErrorMessage = "El nombre del estado de escalamiento es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);

public sealed record EscalationStatusResponseDto(Guid Id, string Name, DateTime CreatedAt, DateTime? UpdatedAt);
