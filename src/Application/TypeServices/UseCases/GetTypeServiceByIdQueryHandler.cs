using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.TypeServices.Entities;
using MediatR;

namespace Application.TypeServices.UseCases;

public sealed class GetTypeServiceByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetTypeServiceByIdQuery, TypeService>
{
    public async Task<TypeService> Handle(
        GetTypeServiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.TypeServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Tipo de servicio no encontrado.");
    }
}
