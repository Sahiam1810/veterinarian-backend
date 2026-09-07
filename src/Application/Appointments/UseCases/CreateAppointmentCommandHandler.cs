using Application.Common.Abstractions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class CreateAppointmentCommandHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences)
    : IRequestHandler<CreateAppointmentCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        Guid appointmentId = default;
        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var locked = await AppointmentSchedulingConcurrency.LockAndEnsureAvailableAsync(
                unitOfWork,
                absences,
                request.AvailabilityId,
                request.ClientPetId,
                request.VeterinarianId,
                request.ScheduledStart,
                request.ScheduledEnd,
                excludeAppointmentId: null,
                request.ConsultingRoom,
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
                request.RequesterPhoneNumber,
                consultingRoom: request.ConsultingRoom ?? locked.ConsultingRoom);

            await unitOfWork.AppointmentsRepository.AddAsync(
                appointment,
                transactionCancellationToken);
            appointmentId = appointment.Id;
        }, cancellationToken);

        return appointmentId;
    }
}
