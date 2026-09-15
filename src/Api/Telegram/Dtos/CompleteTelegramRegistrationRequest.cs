using System.ComponentModel.DataAnnotations;

namespace Api.Telegram.Dtos;

// Solicitud de completado Telegram: sin usuario ni contraseña (Etapa 4.3).
public sealed record CompleteTelegramRegistrationRequest(
    [property: Required, MaxLength(150)] string FullName,
    [property: Required, MaxLength(20)] string IdentificationNumber,
    [property: Required, MaxLength(20)] string PhoneNumber,
    [property: MaxLength(20)] string? Address = null);
