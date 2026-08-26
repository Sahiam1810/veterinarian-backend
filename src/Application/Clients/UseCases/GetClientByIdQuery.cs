using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using MediatR;

namespace Application.Clients.UseCases;

public sealed record GetClientByIdQuery(Guid Id) : IRequest<ClientEntity>;

public sealed class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientEntity>
{
    private readonly IUnitOfWork _uow;

    public GetClientByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ClientEntity> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        var client = await _uow.ClientsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (client is null)
        {
            throw new NotFoundException("Cliente no encontrado.");
        }

        return client;
    }
}
