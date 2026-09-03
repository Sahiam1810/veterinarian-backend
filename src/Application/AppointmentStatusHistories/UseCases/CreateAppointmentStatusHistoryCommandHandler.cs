using Application.Appointments;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.AppointmentStatusHistories.Entities;
using MediatR;

namespace Application.AppointmentStatusHistories.UseCases;

public sealed class CreateAppointmentStatusHistoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAppointmentStatusHistoryCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateAppointmentStatusHistoryCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        if (appointment.ClientPetId != request.ClientPetId)
        {
            throw new BadRequestException("La mascota indicada no corresponde a la cita.");
        }

        var currentStatus = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
            appointment.StatusId, cancellationToken)
            ?? throw new ConflictException("El estado actual de la cita no es válido.");

        var status = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
            request.StatusId, cancellationToken)
            ?? throw new NotFoundException("Estado de cita no encontrado.");

        // Antes se podía crear cualquier transición (incluso CANCELADA -> AGENDADA)
        // sin validar nada, a diferencia del endpoint canónico. Misma regla acá.
        AppointmentStatusTransitionRules.EnsureValidTransition(
            currentStatus.Name,
            status.Name,
            request.Comment);

        var history = new AppointmentStatusHistory(
            request.AppointmentId,
            request.StatusId,
            request.ClientPetId,
            request.Comment);

        await unitOfWork.AppointmentStatusHistoriesRepository.AddAsync(
            history,
            cancellationToken);

        appointment.Update(
            appointment.ClientPetId,
            appointment.VeterinarianId,
            appointment.ServiceId,
            request.StatusId,
            appointment.AvailabilityId,
            appointment.ScheduledStart,
            appointment.ScheduledEnd,
            appointment.Notes);

        await unitOfWork.AppointmentsRepository.UpdateAsync(
            appointment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return history.Id;
    }
}
