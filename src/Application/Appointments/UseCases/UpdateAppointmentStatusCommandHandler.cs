using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.AppointmentStatusHistories.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class UpdateAppointmentStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAppointmentStatusCommand>
{
    public async Task Handle(
        UpdateAppointmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.AppointmentId,
            cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        await AppointmentVeterinarianOwnership.EnsureAsync(
            unitOfWork,
            appointment,
            request.ActorUserAccountId,
            request.EnforceVeterinarianOwnership,
            cancellationToken);

        var currentStatus = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
            appointment.StatusId,
            cancellationToken)
            ?? throw new ConflictException("El estado actual de la cita no es válido.");

        var targetStatus = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
            request.StatusId,
            cancellationToken)
            ?? throw new NotFoundException("Estado de cita no encontrado.");

        AppointmentStatusTransitionRules.EnsureValidTransition(
            currentStatus.Name,
            targetStatus.Name,
            request.Comment);

        var history = new AppointmentStatusHistory(
            request.AppointmentId,
            request.StatusId,
            appointment.ClientPetId,
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
    }
}