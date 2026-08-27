using System.ComponentModel.DataAnnotations;

namespace Api.ConversationStatuses.Dtos;

public sealed record CreateConversationStatusDto(
    [Required(ErrorMessage = "El nombre del estado de conversación es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);

public sealed record UpdateConversationStatusDto(
    [Required(ErrorMessage = "El nombre del estado de conversación es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name);

public sealed record ConversationStatusResponseDto(Guid Id, string Name, DateTime CreatedAt, DateTime? UpdatedAt);
