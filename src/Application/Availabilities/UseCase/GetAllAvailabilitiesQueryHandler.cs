using Application.Common.Abstractions;
using Domain.Availabilities.Entities;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed class GetAllAvailabilitiesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllAvailabilitiesQuery, IReadOnlyCollection<Availability>>
{
    public Task<IReadOnlyCollection<Availability>> Handle(
        GetAllAvailabilitiesQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.AvailabilitiesRepository.GetAllAsync(cancellationToken);
    }
}
