namespace Api.Users.Dtos;

// Perfil Veterinario opcional: el handler lo crea en la misma transacción.
public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    Guid RoleId,
    Guid? SpecialtyId = null,
    string? LicenseNumber = null);
