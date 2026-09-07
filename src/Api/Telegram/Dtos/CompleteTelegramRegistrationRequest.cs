using System.ComponentModel.DataAnnotations;

namespace Api.Telegram.Dtos;

// Solicitud de completado de registro desde Telegram (sin usuario ni contraseña).
public sealed record CompleteTelegramRegistrationRequest(
    [property: Required, MaxLength(150)] string FullName,
    [property: Required, MaxLength(20)] string IdentificationNumber,
    [property: MaxLength(30)] string? PhoneNumber = null,
    [property: MaxLength(200)] string? Address = null);
