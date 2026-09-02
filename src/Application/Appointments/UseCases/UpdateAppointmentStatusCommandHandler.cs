using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.AppointmentStatusHistories.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class UpdateAppointmentStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAppointmentStatusCommand>
{
    private const string Agendada = "AGENDADA";

    private static readonly HashSet<string> AllowedTargetsFromAgendada =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ATENDIDA",
            "CANCELADA",
            "NO_ASISTIO"
        };

    private static readonly HashSet<string> CommentRequiredTargets =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CANCELADA",
            "NO_ASISTIO"
        };

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

        if (!string.Equals(currentStatus.Name, Agendada, StringComparison.OrdinalIgnoreCase)
            || !AllowedTargetsFromAgendada.Contains(targetStatus.Name))
        {
            throw new ConflictException("La transición de estado solicitada no está permitida.");
        }

        if (CommentRequiredTargets.Contains(targetStatus.Name)
            && string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new BadRequestException("El comentario es requerido para este estado.");
        }

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