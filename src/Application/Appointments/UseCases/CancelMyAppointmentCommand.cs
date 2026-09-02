using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

// Autoservicio autenticado: cancela la propia cita por cambio de estado (soft-cancel).
public sealed record CancelMyAppointmentCommand(
    Guid AppointmentId,
    Guid UserAccountId,
    string? Comment) : IRequest;

public sealed class CancelMyAppointmentCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CancelMyAppointmentCommand>
{
    private const string Agendada = "AGENDADA";
    private const string Cancelada = "CANCELADA";

    public async Task Handle(
        CancelMyAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
            request.UserAccountId,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var client = await unitOfWork.ClientsRepository.GetByUserIdAsync(
            account.UserId,
            cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");

        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.AppointmentId,
            cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(
            client.Id,
            cancellationToken);
        if (clientPets.All(cp => cp.Id != appointment.ClientPetId))
        {
            throw new ForbiddenException("La cita no pertenece al cliente autenticado.");
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
            ? "Cancelada por el cliente autenticado."
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
