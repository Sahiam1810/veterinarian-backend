using MediatR;

namespace Application.Users.UseCase;

public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string Password,
    Guid RoleId) : IRequest<Guid>;
