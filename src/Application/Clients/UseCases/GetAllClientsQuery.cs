using Application.Common.Abstractions;
using Domain.Clients.Entities;
using MediatR;

namespace Application.Clients.UseCases;

public sealed record GetAllClientsQuery() : IRequest<IReadOnlyCollection<ClientEntity>>;

public sealed class GetAllClientsQueryHandler : IRequestHandler<GetAllClientsQuery, IReadOnlyCollection<ClientEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllClientsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<ClientEntity>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken)
    {
        return await _uow.ClientsRepository.GetAllAsync(cancellationToken);
    }
}
