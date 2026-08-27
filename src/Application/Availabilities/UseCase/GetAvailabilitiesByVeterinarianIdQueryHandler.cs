using Application.Common.Abstractions;
using Domain.Availabilities.Entities;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed class GetAvailabilitiesByVeterinarianIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<
        GetAvailabilitiesByVeterinarianIdQuery,
        IReadOnlyCollection<Availability>>
{
    public Task<IReadOnlyCollection<Availability>> Handle(
        GetAvailabilitiesByVeterinarianIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.AvailabilitiesRepository.GetAllByVeterinarianIdAsync(
            request.VeterinarianId,
            cancellationToken);
    }
}
