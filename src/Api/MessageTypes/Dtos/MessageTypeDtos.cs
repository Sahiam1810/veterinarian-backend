using System.ComponentModel.DataAnnotations;

namespace Api.MessageTypes.Dtos;

public sealed record CreateMessageTypeDto(
    [Required(ErrorMessage = "El nombre del tipo de mensaje es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);

public sealed record UpdateMessageTypeDto(
    [Required(ErrorMessage = "El nombre del tipo de mensaje es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);

public sealed record MessageTypeResponseDto(Guid Id, string Name, DateTime CreatedAt, DateTime? UpdatedAt);
