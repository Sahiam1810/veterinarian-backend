using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using MediatR;

namespace Application.Clients.UseCases;

// Resuelve cliente por cédula para bootstrap del chatbot (sin JWT).
public sealed record GetClientByIdentificationQuery(string IdentificationNumber)
    : IRequest<ClientEntity>;

public sealed class GetClientByIdentificationQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetClientByIdentificationQuery, ClientEntity>
{
    public async Task<ClientEntity> Handle(
        GetClientByIdentificationQuery request,
        CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientsRepository.GetByIdentificationNumberAsync(
            request.IdentificationNumber,
            cancellationToken);

        return client ?? throw new NotFoundException("Cliente no encontrado.");
    }
}
