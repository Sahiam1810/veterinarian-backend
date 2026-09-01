using MediatR;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;

namespace Application.UserCredentials.UseCase;

public sealed record GetUserCredentialsByAccountIdQuery(Guid AccountId)
    : IRequest<UserCredentialsEntity>;
