using System.ComponentModel.DataAnnotations;

namespace Api.Clients.Dtos;

// Alta de cliente por el personal (POST /api/clients). El cliente no tiene usuario.
public record CreateClientDto(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre completo no puede superar los 150 caracteres.")]
    string FullName,

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El correo electrónico no puede superar los 150 caracteres.")]
    string Email,

    [Required(ErrorMessage = "El número de identificación es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El número de identificación no puede superar los 20 caracteres.")]
    string IdentificationNumber,

    // Obligatoriedad y formato los resuelve FluentValidation (codes Clients.Phone*).
    string? PhoneNumber,

    [MaxLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
    string? Address = null
);

// Edición de cliente (PUT /api/clients/{id}): mismos campos que el alta más el estado.
public record UpdateClientDto(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre completo no puede superar los 150 caracteres.")]
    string FullName,

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El correo electrónico no puede superar los 150 caracteres.")]
    string Email,

    [Required(ErrorMessage = "El número de identificación es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El número de identificación no puede superar los 20 caracteres.")]
    string IdentificationNumber,

    // Update no deja el teléfono vacío: misma regla FluentValidation que Create.
    string? PhoneNumber,

    [MaxLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
    string? Address,

    [Required(ErrorMessage = "El estado del cliente es obligatorio.")]
    bool? IsActive
);

public record ClientResponseDto(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    string IdentificationNumber,
    string PhoneNumber,
    string? Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// Respuesta acotada para el lookup anónimo por cédula: sin Address/PhoneNumber.
// Ese endpoint solo existe para que el chatbot ubique al cliente antes de
// tener JWT -- no requiere devolver PII de contacto, y cualquiera que
// conozca un número de identificación válido puede llamarlo.
public record ClientIdentificationLookupResponseDto(
    Guid Id,
    string IdentificationNumber,
    DateTime CreatedAt
);

// Mismo contrato acotado que by-identification (sin Address ni PhoneNumber).
// El caller ya conoce el teléfono; no se reexpone PII de contacto.
public record ClientPhoneLookupResponseDto(
    Guid Id,
    string IdentificationNumber,
    DateTime CreatedAt
);
