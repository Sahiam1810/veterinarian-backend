using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class UpdateAppointmentCommandHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences)
    : IRequestHandler<UpdateAppointmentCommand>
{
    public async Task Handle(
        UpdateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Cita medica no encontrada.");

        await AppointmentVeterinarianOwnership.EnsureAsync(
            unitOfWork,
            appointment,
            request.ActorUserAccountId,
            request.EnforceVeterinarianOwnership,
            cancellationToken);

        // Sin campo phone en el comando: si el dueño tiene telefono en perfil, realinear.
        var requesterPhone = await AppointmentRequesterPhonePolicy.ResolveAsync(
            unitOfWork,
            request.ClientPetId,
            requestPhoneNumber: null,
            requirePhone: false,
            cancellationToken);

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
                request.Id,
                request.ConsultingRoom,
                transactionCancellationToken);

            appointment.Update(
                request.ClientPetId,
                request.VeterinarianId,
                request.ServiceId,
                appointment.StatusId,
                request.AvailabilityId,
                request.ScheduledStart,
                request.ScheduledEnd,
                request.Notes,
                request.ConsultingRoom ?? locked.ConsultingRoom);

            if (requesterPhone is not null)
            {
                appointment.ApplyRequesterPhone(requesterPhone);
            }

            await unitOfWork.AppointmentsRepository.UpdateAsync(
                appointment,
                transactionCancellationToken);
        }, cancellationToken);
    }
}