using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed class UpdateAvailabilityCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAvailabilityCommand>
{
    public async Task Handle(
        UpdateAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var availability = await unitOfWork.AvailabilitiesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Disponibilidad no encontrada.");

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
    }
}
