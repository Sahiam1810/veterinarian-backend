using Application.Common.Abstractions;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed class UpdateAvailabilityCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAvailabilityCommand, bool>
{
    public async Task<bool> Handle(
        UpdateAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var availability = await unitOfWork.AvailabilitiesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (availability is null)
        {
            return false;
        }

        availability.Update(
            request.VeterinarianId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.IsActive);

        await unitOfWork.AvailabilitiesRepository.UpdateAsync(
            availability,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
