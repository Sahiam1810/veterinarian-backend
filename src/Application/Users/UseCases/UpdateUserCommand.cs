using MediatR;

namespace Application.Users.UseCase;

public sealed record UpdateUserCommand(
    Guid Id,
    string FullName,
    string Email,
    Guid RoleId) : IRequest<bool>;
