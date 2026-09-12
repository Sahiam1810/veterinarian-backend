namespace Api.Users.Dtos;

// Perfil Cliente/Veterinario opcional: el handler los crea en la misma transacción (S26).
public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string? Password,
    Guid RoleId,
    string? ClientIdentificationNumber = null,
    string? ClientPhoneNumber = null,
    string? ClientAddress = null,
    Guid? SpecialtyId = null,
    string? LicenseNumber = null);
