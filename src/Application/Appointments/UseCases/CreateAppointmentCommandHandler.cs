using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class CreateAppointmentCommandHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences,
    TimeProvider timeProvider)
    : IRequestHandler<CreateAppointmentCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        // S36: el staff no puede agendar en una fecha/hora que ya pasó.
        AppointmentPastDateGuard.EnsureNotInThePast(request.ScheduledStart, timeProvider);

        // Telefono: perfil del dueño gana; si no hay, request obligatorio.
        var requesterPhone = await AppointmentRequesterPhonePolicy.ResolveAsync(
            unitOfWork,
            request.ClientPetId,
            request.RequesterPhoneNumber,
            requirePhone: true,
            cancellationToken);

        // Servicio inactivo: citas viejas siguen; no se asigna a nuevas.
        var service = await unitOfWork.ServicesRepository.GetByIdAsync(
            request.ServiceId,
            cancellationToken)
            ?? throw new NotFoundException("Servicio no encontrado.");
        if (!service.IsActive)
        {
            throw new BadRequestException(
                "El servicio no está disponible para citas nuevas.");
        }

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
                requesterPhone,
                consultingRoom: request.ConsultingRoom ?? locked.ConsultingRoom);

            await unitOfWork.AppointmentsRepository.AddAsync(
                appointment,
                transactionCancellationToken);
            appointmentId = appointment.Id;
        }, cancellationToken);

        return appointmentId;
    }
}
