using MediatR;

namespace Application.Users.UseCase;

// Password es nullable: los usuarios con rol Cliente nunca se loguean (solo
// interactúan vía chatbot) y por lo tanto no reciben contraseña.
// Campos de perfil opcionales: al crear Cliente/Veterinario se provisiona la fila
// en la misma transacción (S26); si no vienen, se generan valores únicos seguros.
public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string? Password,
    Guid RoleId,
    string? ClientIdentificationNumber = null,
    string? ClientPhoneNumber = null,
    string? ClientAddress = null,
    Guid? SpecialtyId = null,
    string? LicenseNumber = null) : IRequest<Guid>;
