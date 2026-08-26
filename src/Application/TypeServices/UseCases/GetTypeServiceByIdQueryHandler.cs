using Application.Common.Abstractions;
using Domain.TypeServices.Entities;
using MediatR;

namespace Application.TypeServices.UseCases;

public sealed class GetTypeServiceByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetTypeServiceByIdQuery, TypeService?>
{
    public Task<TypeService?> Handle(
        GetTypeServiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.TypeServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
