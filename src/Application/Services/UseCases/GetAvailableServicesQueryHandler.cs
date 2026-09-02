using Application.Common.Abstractions;
using Domain.Services.Entities;
using MediatR;

namespace Application.Services.UseCases;

public sealed class GetAvailableServicesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAvailableServicesQuery, IReadOnlyCollection<Service>>
{
    public Task<IReadOnlyCollection<Service>> Handle(
        GetAvailableServicesQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.ServicesRepository.GetAvailableAsync(cancellationToken);
    }
}
