using MediatR;

namespace Application.UserAccounts.UseCase;

public sealed record UpdateUserAccountCommand(
    Guid Id,
    string Username,
    string Mail,
    string Status) : IRequest;
