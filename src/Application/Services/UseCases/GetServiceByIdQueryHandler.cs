using Application.Common.Abstractions;
using Domain.Services.Entities;
using MediatR;

namespace Application.Services.UseCases;

public sealed class GetServiceByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetServiceByIdQuery, Service?>
{
    public Task<Service?> Handle(
        GetServiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.ServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
