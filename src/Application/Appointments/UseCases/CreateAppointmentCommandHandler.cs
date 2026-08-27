using Application.Common.Abstractions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class CreateAppointmentCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAppointmentCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = new Appointment(
            request.ClientPetId,
            request.VeterinarianId,
            request.ServiceId,
            request.StatusId,
            request.Reason,
            request.ScheduledStart,
            request.ScheduledEnd,
            request.Notes);

        await unitOfWork.AppointmentsRepository.AddAsync(
            appointment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return appointment.Id;
    }
}
