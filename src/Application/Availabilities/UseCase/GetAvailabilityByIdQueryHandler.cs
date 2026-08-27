using Application.Common.Abstractions;
using Domain.Availabilities.Entities;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed class GetAvailabilityByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAvailabilityByIdQuery, Availability?>
{
    public Task<Availability?> Handle(
        GetAvailabilityByIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.AvailabilitiesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
