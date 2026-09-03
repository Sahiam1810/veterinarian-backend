using MediatR;

namespace Application.Users.UseCase;

// Password es nullable: los usuarios con rol Cliente nunca se loguean (solo
// interactúan vía chatbot) y por lo tanto no reciben contraseña.
public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string? Password,
    Guid RoleId) : IRequest<Guid>;
