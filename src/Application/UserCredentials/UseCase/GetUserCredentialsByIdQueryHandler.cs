using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;

namespace Application.UserCredentials.UseCase;

public sealed class GetUserCredentialsByIdQueryHandler
    : IRequestHandler<GetUserCredentialsByIdQuery, UserCredentialsEntity>
{
    private readonly IUnitOfWork _uow;

    public GetUserCredentialsByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<UserCredentialsEntity> Handle(
        GetUserCredentialsByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.UserCredentialsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Credenciales no encontradas.");
    }
}
