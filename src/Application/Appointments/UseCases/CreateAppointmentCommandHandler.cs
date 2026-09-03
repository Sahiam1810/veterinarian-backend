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
        Guid appointmentId = default;
        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await AppointmentSchedulingConcurrency.LockAndEnsureAvailableAsync(
                unitOfWork,
                request.AvailabilityId,
                request.ClientPetId,
                request.VeterinarianId,
                request.ScheduledStart,
                request.ScheduledEnd,
                excludeAppointmentId: null,
                transactionCancellationToken);

            var appointment = new Appointment(
                request.ClientPetId,
                request.VeterinarianId,
                request.ServiceId,
                request.StatusId,
                request.AvailabilityId,
                request.ScheduledStart,
                request.ScheduledEnd,
                request.Notes,
                request.RequesterPhoneNumber);

            await unitOfWork.AppointmentsRepository.AddAsync(
                appointment,
                transactionCancellationToken);
            appointmentId = appointment.Id;
        }, cancellationToken);

        return appointmentId;
    }
}
