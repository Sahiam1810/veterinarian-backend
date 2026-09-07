using Application.Clients.Abstraction;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using MediatR;

namespace Application.Clients.UseCases;

public class GetClientLookupQueryHandler(IClientRepository clientRepository)
    : IRequestHandler<GetClientLookupQuery, ClientEntity>
{
    public async Task<ClientEntity> Handle(GetClientLookupQuery request, CancellationToken cancellationToken)
    {
        var client = await clientRepository.GetByLookupAsync(
            request.IdentificationNumber,
            request.PhoneNumber,
            cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Cliente no encontrado con los criterios especificados.");
        }

        return client;
    }
}
