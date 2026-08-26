using MediatR;

namespace Application.UserAccounts.UseCase;

public sealed record CreateUserAccountCommand(
    Guid UserId,
    string Username,
    string Mail,
    string Status) : IRequest<Guid>;
