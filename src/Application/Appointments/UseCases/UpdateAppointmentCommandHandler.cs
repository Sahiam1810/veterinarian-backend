using Application.Common.Abstractions;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class UpdateAppointmentCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAppointmentCommand, bool>
{
    public async Task<bool> Handle(
        UpdateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (appointment is null)
        {
            return false;
        }

        appointment.Update(
            request.ClientPetId,
            request.VeterinarianId,
            request.ServiceId,
            request.StatusId,
            request.AvailabilityId,
            request.ScheduledStart,
            request.ScheduledEnd,
            request.Notes);

        await unitOfWork.AppointmentsRepository.UpdateAsync(
            appointment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
