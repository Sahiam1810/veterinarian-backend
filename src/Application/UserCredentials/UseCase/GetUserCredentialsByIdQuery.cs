using MediatR;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;

namespace Application.UserCredentials.UseCase;

public sealed record GetUserCredentialsByIdQuery(Guid Id)
    : IRequest<UserCredentialsEntity>;
