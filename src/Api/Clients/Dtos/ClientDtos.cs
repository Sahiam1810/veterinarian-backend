using System.ComponentModel.DataAnnotations;

namespace Api.Clients.Dtos;

public record CreateClientDto(
    [Required(ErrorMessage = "El ID de usuario es obligatorio.")]
    Guid UserId,

    [Required(ErrorMessage = "El número de identificación es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El número de identificación no puede superar los 20 caracteres.")]
    string IdentificationNumber,

    [MaxLength(20, ErrorMessage = "La dirección no puede superar los 20 caracteres.")]
    string? Address,

    DateTime? RegistrationDate = null,

    [MaxLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    string? PhoneNumber = null
);

public record UpdateClientDto(
    [Required(ErrorMessage = "El ID de usuario es obligatorio.")]
    Guid UserId,

    [Required(ErrorMessage = "El número de identificación es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El número de identificación no puede superar los 20 caracteres.")]
    string IdentificationNumber,

    [MaxLength(20, ErrorMessage = "La dirección no puede superar los 20 caracteres.")]
    string? Address,

    DateTime? RegistrationDate = null,

    [MaxLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    string? PhoneNumber = null
);

public record ClientResponseDto(
    Guid Id,
    Guid UserId,
    string IdentificationNumber,
    string? Address,
    string? PhoneNumber,
    DateTime RegistrationDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// Respuesta acotada para el lookup anónimo por cédula: sin Address/PhoneNumber.
// Ese endpoint solo existe para que el chatbot ubique al cliente antes de
// tener JWT -- no requiere devolver PII de contacto, y cualquiera que
// conozca un número de identificación válido puede llamarlo.
public record ClientIdentificationLookupResponseDto(
    Guid Id,
    Guid UserId,
    string IdentificationNumber,
    DateTime RegistrationDate
);
