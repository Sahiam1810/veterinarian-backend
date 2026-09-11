using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using MediatR;

namespace Application.Appointments.UseCases;

// Reprograma una cita existente; solo permite estado AGENDADA (S27).
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

        // Solo AGENDADA admite reprogramar; Atendida/Cancelada/No Asistió quedan cerradas.
        var currentStatus = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
            appointment.StatusId,
            cancellationToken)
            ?? throw new ConflictException("El estado actual de la cita no es válido.");
        if (!string.Equals(
                currentStatus.Name,
                AppointmentStatusNames.Agendada,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                "No se puede reprogramar una cita que ya fue atendida, cancelada o marcada como no asistida.");
        }

        // No reasignar a un servicio inactivo (las citas que ya lo tenían se conservan).
        if (request.ServiceId != appointment.ServiceId)
        {
            var service = await unitOfWork.ServicesRepository.GetByIdAsync(
                request.ServiceId,
                cancellationToken)
                ?? throw new NotFoundException("Servicio no encontrado.");
            if (!service.IsActive)
            {
                throw new BadRequestException(
                    "El servicio no está disponible para citas nuevas.");
            }
        }

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