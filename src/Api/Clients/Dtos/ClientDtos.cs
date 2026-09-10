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

    // Obligatoriedad y formato los resuelve FluentValidation (codes Clients.Phone*).
    string? PhoneNumber,

    DateTime? RegistrationDate = null
);

public record UpdateClientDto(
    [Required(ErrorMessage = "El ID de usuario es obligatorio.")]
    Guid UserId,

    [Required(ErrorMessage = "El número de identificación es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El número de identificación no puede superar los 20 caracteres.")]
    string IdentificationNumber,

    [MaxLength(20, ErrorMessage = "La dirección no puede superar los 20 caracteres.")]
    string? Address,

    // Update no deja el teléfono vacío: misma regla FluentValidation que Create.
    string? PhoneNumber,

    DateTime? RegistrationDate = null
);

public record ClientResponseDto(
    Guid Id,
    Guid UserId,
    string IdentificationNumber,
    string? Address,
    string? PhoneNumber,
    DateTime RegistrationDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    // Nombre y correo resueltos desde la navegación User — disponibles sin
    // necesidad de una segunda llamada a /api/Users por parte del cliente.
    // Nullable: si el User vinculado no se cargó (no debería ocurrir) no
    // revienta la serialización.
    string? FullName,
    string? Email
);

// Tarea 4.1: alta staff de dueño/cliente. Sin password/credentials -- no otorga acceso.
public record RegisterOwnerDto(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre completo no puede superar los 150 caracteres.")]
    string FullName,

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El correo electrónico no puede superar los 150 caracteres.")]
    string Email,

    [Required(ErrorMessage = "El número de identificación es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El número de identificación no puede superar los 20 caracteres.")]
    string IdentificationNumber,

    // Obligatoriedad y formato los resuelve FluentValidation (RegisterOwnerCommandValidator).
    string PhoneNumber,

    [MaxLength(20, ErrorMessage = "La dirección no puede superar los 20 caracteres.")]
    string? Address = null,

    // Solo se consume si RegisterOwner:RequireContactProofs está activo (hoy false para Staff).
    Guid? ContactProofSessionId = null,
    string? ContactProof = null
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

// Mismo contrato acotado que by-identification (sin Address ni PhoneNumber).
// El caller ya conoce el teléfono; no se reexpone PII de contacto.
public record ClientPhoneLookupResponseDto(
    Guid Id,
    Guid UserId,
    string IdentificationNumber,
    DateTime RegistrationDate
);
