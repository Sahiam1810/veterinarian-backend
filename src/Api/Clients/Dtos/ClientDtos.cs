using System.ComponentModel.DataAnnotations;

namespace Api.Clients.Dtos;

// Alta de cliente por el personal (POST /api/clients). El cliente no tiene usuario.
public record CreateClientDto(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre completo no puede superar los 150 caracteres.")]
    string FullName,

    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El correo electronico no puede superar los 150 caracteres.")]
    string Email,

    [Required(ErrorMessage = "El numero de identificacion es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El numero de identificacion no puede superar los 20 caracteres.")]
    string IdentificationNumber,

    // Obligatoriedad y formato los resuelve FluentValidation (codes Clients.Phone*).
    string? PhoneNumber,

    [MaxLength(150, ErrorMessage = "La direccion no puede superar los 150 caracteres.")]
    string? Address = null
);

// Edicion de cliente (PUT /api/clients/{id}): mismos campos que el alta mas el estado.
public record UpdateClientDto(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre completo no puede superar los 150 caracteres.")]
    string FullName,

    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El correo electronico no puede superar los 150 caracteres.")]
    string Email,

    [Required(ErrorMessage = "El numero de identificacion es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El numero de identificacion no puede superar los 20 caracteres.")]
    string IdentificationNumber,

    // Update no deja el telefono vacio: misma regla FluentValidation que Create.
    string? PhoneNumber,

    [MaxLength(150, ErrorMessage = "La direccion no puede superar los 150 caracteres.")]
    string? Address,

    [Required(ErrorMessage = "El estado del cliente es obligatorio.")]
    bool? IsActive
);

public record ClientResponseDto(
    Guid Id,
    string FullName,
    string? Email,
    bool IsActive,
    string? IdentificationNumber,
    // Puede ser null al leer historicos invalidos; create/update siguen exigiendo telefono.
    string? PhoneNumber,
    string? Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// Respuesta acotada para el lookup anonimo por cedula.
public record ClientIdentificationLookupResponseDto(
    Guid Id,
    string? IdentificationNumber,
    DateTime CreatedAt
);

// Mismo contrato acotado que by-identification.
public record ClientPhoneLookupResponseDto(
    Guid Id,
    string? IdentificationNumber,
    DateTime CreatedAt
);
