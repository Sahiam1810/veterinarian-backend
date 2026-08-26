using System.ComponentModel.DataAnnotations;

namespace Api.AiRunStatuses.Dtos;

public sealed record CreateAiRunStatusDto(
    [Required(ErrorMessage = "El nombre del estado es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string NameStatus);

public sealed record UpdateAiRunStatusDto(
    [Required(ErrorMessage = "El nombre del estado es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string NameStatus);

public sealed record AiRunStatusResponseDto(Guid Id, string NameStatus, DateTime CreatedAt, DateTime? UpdatedAt);
