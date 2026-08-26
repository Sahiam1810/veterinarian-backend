using System.ComponentModel.DataAnnotations;

namespace Api.SenderTypes.Dtos;

public sealed record CreateSenderTypeDto(
    [Required(ErrorMessage = "El nombre del tipo de remitente es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);

public sealed record UpdateSenderTypeDto(
    [Required(ErrorMessage = "El nombre del tipo de remitente es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);

public sealed record SenderTypeResponseDto(Guid Id, string Name, DateTime CreatedAt, DateTime? UpdatedAt);
