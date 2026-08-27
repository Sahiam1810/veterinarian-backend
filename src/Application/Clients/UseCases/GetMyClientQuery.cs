using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using MediatR;

namespace Application.Clients.UseCases;

public sealed record GetMyClientQuery(Guid UserAccountId) : IRequest<ClientEntity>;

public sealed class GetMyClientQueryHandler : IRequestHandler<GetMyClientQuery, ClientEntity>
{
    private readonly IUnitOfWork _uow;

    public GetMyClientQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ClientEntity> Handle(GetMyClientQuery request, CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(request.UserAccountId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException("Cuenta de usuario no encontrada.");
        }

        var client = await _uow.ClientsRepository.GetByUserIdAsync(account.UserId, cancellationToken);
        if (client is null)
        {
            throw new NotFoundException("El usuario autenticado no tiene un perfil de cliente asociado.");
        }

        return client;
    }
}
