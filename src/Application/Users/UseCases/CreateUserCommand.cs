using MediatR;

namespace Application.Users.UseCase;

// Password siempre requerida: USERS es solo personal.
// Campos de perfil opcionales de veterinario: al crear Veterinario se provisiona
// la fila en la misma transacción; si no vienen, se generan valores únicos seguros.
public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string Password,
    Guid RoleId,
    Guid? SpecialtyId = null,
    string? LicenseNumber = null) : IRequest<Guid>;
