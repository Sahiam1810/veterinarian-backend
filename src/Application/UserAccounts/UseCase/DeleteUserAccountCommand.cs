using MediatR;

namespace Application.UserAccounts.UseCase;

public sealed record DeleteUserAccountCommand(Guid Id) : IRequest;
