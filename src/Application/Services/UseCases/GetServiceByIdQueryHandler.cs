using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Services.Entities;
using MediatR;

namespace Application.Services.UseCases;

public sealed class GetServiceByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetServiceByIdQuery, Service>
{
    public async Task<Service> Handle(
        GetServiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.ServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Servicio no encontrado.");
    }
}
