using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record CancelAppointmentByIdentificationCommand(
    Guid AppointmentId,
    string IdentificationNumber,
    string? Comment) : IRequest;

public sealed class CancelAppointmentByIdentificationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CancelAppointmentByIdentificationCommand>
{
    private const string Agendada = AppointmentStatusNames.Agendada;
    private const string Cancelada = "CANCELADA";

    public async Task Handle(
        CancelAppointmentByIdentificationCommand request,
        CancellationToken cancellationToken)
    {
        var client = await GetAppointmentsByIdentificationQueryHandler.ResolveClientOrNotFoundAsync(
            unitOfWork,
            request.IdentificationNumber,
            cancellationToken);

        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.AppointmentId,
            cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(
            client.Id,
            cancellationToken);
        if (clientPets.All(cp => cp.Id != appointment.ClientPetId))
        {
            throw new ForbiddenException("La cita no pertenece al cliente de esa cédula.");
        }

        var currentStatus = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
            appointment.StatusId,
            cancellationToken)
            ?? throw new ConflictException("El estado actual de la cita no es válido.");

        if (!string.Equals(currentStatus.Name, Agendada, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Solo se puede cancelar una cita en estado AGENDADA.");
        }

        var statuses = await unitOfWork.StatusAppointmentsRepository.GetAllAsync(cancellationToken);
        var cancelStatus = statuses.FirstOrDefault(s =>
            string.Equals(s.Name, Cancelada, StringComparison.OrdinalIgnoreCase))
            ?? throw new ConflictException("No está configurado el estado CANCELADA.");

        var comment = string.IsNullOrWhiteSpace(request.Comment)
            ? "Cancelada por el cliente (bot, cédula)."
            : request.Comment;

        var history = new Domain.AppointmentStatusHistories.Entities.AppointmentStatusHistory(
            appointment.Id,
            cancelStatus.Id,
            appointment.ClientPetId,
            comment);

        await unitOfWork.AppointmentStatusHistoriesRepository.AddAsync(history, cancellationToken);

        appointment.Update(
            appointment.ClientPetId,
            appointment.VeterinarianId,
            appointment.ServiceId,
            cancelStatus.Id,
            appointment.AvailabilityId,
            appointment.ScheduledStart,
            appointment.ScheduledEnd,
            appointment.Notes);

        await unitOfWork.AppointmentsRepository.UpdateAsync(appointment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
