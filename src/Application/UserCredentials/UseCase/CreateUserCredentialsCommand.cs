using MediatR;

namespace Application.UserCredentials.UseCase;

public sealed record CreateUserCredentialsCommand(
    Guid AccountId,
    string Password) : IRequest<Guid>;
