using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Appointments.ValueObjects;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record RescheduleMyAppointmentCommand(
    Guid AppointmentId,
    Guid UserAccountId,
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string RequesterPhoneNumber,
    string? Notes) : IRequest;

public sealed class RescheduleMyAppointmentCommandHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences)
    : IRequestHandler<RescheduleMyAppointmentCommand>
{
    public async Task Handle(
        RescheduleMyAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = RequesterPhoneNumber.Normalize(request.RequesterPhoneNumber);
        if (normalizedPhone.Length is < 7 or > RequesterPhoneNumber.MaxLength)
        {
            throw new BadRequestException(
                $"El teléfono de contacto debe tener entre 7 y {RequesterPhoneNumber.MaxLength} dígitos.");
        }

        if (request.ScheduledEnd <= request.ScheduledStart)
        {
            throw new BadRequestException("La franja horaria de reagendado no es válida.");
        }

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
        if (clientPets.All(clientPet => clientPet.Id != appointment.ClientPetId))
        {
            throw new ForbiddenException("La cita no pertenece al cliente autenticado.");
        }

        var currentStatus = await unitOfWork.StatusAppointmentsRepository.GetByIdAsync(
            appointment.StatusId,
            cancellationToken)
            ?? throw new ConflictException("El estado actual de la cita no es válido.");

        if (!string.Equals(
                currentStatus.Name,
                AppointmentStatusNames.Agendada,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Solo se puede reagendar una cita en estado AGENDADA.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var availability = await AppointmentSchedulingConcurrency.LockAndEnsureAvailableAsync(
                unitOfWork,
                absences,
                request.AvailabilityId,
                appointment.ClientPetId,
                appointment.VeterinarianId,
                request.ScheduledStart,
                request.ScheduledEnd,
                appointment.Id,
                consultingRoom: null,
                transactionCancellationToken);

            appointment.Reschedule(
                request.AvailabilityId,
                request.ScheduledStart,
                request.ScheduledEnd,
                request.Notes,
                availability.ConsultingRoom);
            appointment.ApplyRequesterPhone(normalizedPhone);

            await unitOfWork.AppointmentsRepository.UpdateAsync(
                appointment,
                transactionCancellationToken);
            await unitOfWork.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);
    }
}

