using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using MediatR;

namespace Application.Clients.UseCases;

// Resuelve cliente por teléfono para bootstrap del chatbot (sin JWT).
public sealed record GetClientByPhoneQuery(string PhoneNumber)
    : IRequest<ClientEntity>;

public sealed class GetClientByPhoneQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetClientByPhoneQuery, ClientEntity>
{
    public async Task<ClientEntity> Handle(
        GetClientByPhoneQuery request,
        CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientsRepository.GetByPhoneAsync(
            request.PhoneNumber,
            cancellationToken);

        return client ?? throw new NotFoundException("Cliente no encontrado.");
    }
}
