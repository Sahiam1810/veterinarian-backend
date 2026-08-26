using Application.Common.Abstractions;
using Domain.Services.Entities;
using MediatR;

namespace Application.Services.UseCases;

public sealed class GetAllServicesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllServicesQuery, IReadOnlyCollection<Service>>
{
    public Task<IReadOnlyCollection<Service>> Handle(
        GetAllServicesQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.ServicesRepository.GetAllAsync(cancellationToken);
    }
}
