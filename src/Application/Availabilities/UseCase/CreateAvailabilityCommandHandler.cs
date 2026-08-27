using Application.Common.Abstractions;
using Domain.Availabilities.Entities;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed class CreateAvailabilityCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAvailabilityCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var availability = new Availability(
            request.VeterinarianId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.IsActive);

        await unitOfWork.AvailabilitiesRepository.AddAsync(
            availability,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return availability.Id;
    }
}
