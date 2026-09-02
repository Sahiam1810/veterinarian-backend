using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class UpdateAppointmentCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAppointmentCommand>
{
    public async Task Handle(
        UpdateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        appointment.Update(
            request.ClientPetId,
            request.VeterinarianId,
            request.ServiceId,
            appointment.StatusId,
            request.AvailabilityId,
            request.ScheduledStart,
            request.ScheduledEnd,
            request.Notes);

        await unitOfWork.AppointmentsRepository.UpdateAsync(
            appointment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
