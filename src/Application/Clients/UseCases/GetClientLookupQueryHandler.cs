using Application.Clients.Abstraction;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Clients.UseCases;

public class GetClientLookupQueryHandler(
    IClientRepository clientRepository,
    ILogger<GetClientLookupQueryHandler> logger)
    : IRequestHandler<GetClientLookupQuery, ClientEntity>
{
    public async Task<ClientEntity> Handle(GetClientLookupQuery request, CancellationToken cancellationToken)
    {
        // Solo el modo de búsqueda (Phone/Identification), nunca el valor crudo.
        var mode = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? "Phone" : "Identification";

        var client = await clientRepository.GetByLookupAsync(
            request.IdentificationNumber,
            request.PhoneNumber,
            cancellationToken);

        if (client is null)
        {
            logger.LogInformation("Client lookup not found. Mode={Mode}", mode);
            throw new NotFoundException("Cliente no encontrado con los criterios especificados.");
        }

        logger.LogInformation(
            "Client lookup succeeded. ClientId={ClientId} Mode={Mode}",
            client.Id,
            mode);

        return client;
    }
}
