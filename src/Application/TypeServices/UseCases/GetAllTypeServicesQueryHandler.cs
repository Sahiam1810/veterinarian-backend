using Application.Common.Abstractions;
using Domain.TypeServices.Entities;
using MediatR;

namespace Application.TypeServices.UseCases;

public sealed class GetAllTypeServicesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllTypeServicesQuery, IReadOnlyCollection<TypeService>>
{
    public Task<IReadOnlyCollection<TypeService>> Handle(
        GetAllTypeServicesQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.TypeServicesRepository.GetAllAsync(cancellationToken);
    }
}
