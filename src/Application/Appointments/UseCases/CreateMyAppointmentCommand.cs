using System.Security.Cryptography;
using System.Text;
using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record CreateMyAppointmentCommand(
    Guid UserAccountId,
    Guid PetId,
    Guid VeterinarianId,
    Guid ServiceId,
    DateTime ScheduledStartUtc,
    string? Notes,
    string? RequesterPhoneNumber,
    string IdempotencyKey) : IRequest<Appointment>;

public sealed class CreateMyAppointmentCommandHandler(
    IUnitOfWork unitOfWork,
    IAppointmentBookingSettings settings,
    TimeProvider timeProvider)
    : IRequestHandler<CreateMyAppointmentCommand, Appointment>
{
    public async Task<Appointment> Handle(
        CreateMyAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
            request.UserAccountId,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");
        var client = await unitOfWork.ClientsRepository.GetByUserIdAsync(
            account.UserId,
            cancellationToken)
            ?? throw new NotFoundException("El usuario no tiene un perfil de cliente asociado.");
        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(
            client.Id,
            cancellationToken);
        var clientPet = clientPets.SingleOrDefault(item => item.PetId == request.PetId)
            ?? throw new NotFoundException("La mascota no pertenece al cliente autenticado.");
        var service = await unitOfWork.ServicesRepository.GetByIdAsync(
            request.ServiceId,
            cancellationToken)
            ?? throw new NotFoundException("Servicio no encontrado.");
        if (!service.IsActive)
        {
            throw new BadRequestException("El servicio no está disponible.");
        }
        var veterinarian = await unitOfWork.VeterinariansRepository.GetByIdAsync(
            request.VeterinarianId,
            cancellationToken)
            ?? throw new NotFoundException("Veterinario no encontrado.");
        if (veterinarian.User?.IsActive != true)
        {
            throw new BadRequestException("El veterinario no está disponible.");
        }

        var phone = client.PhoneNumber?.Value ?? request.RequesterPhoneNumber;
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new BadRequestException("El teléfono del solicitante es requerido.");
        }
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var hash = HashKey(request.UserAccountId, request.IdempotencyKey.Trim());
        var endUtc = request.ScheduledStartUtc.AddMinutes(service.DurationMinutes);
        var availability = await ResolveAvailabilityAsync(
            request.VeterinarianId,
            request.ScheduledStartUtc,
            endUtc,
            service.DurationMinutes,
            cancellationToken);
        ValidateBookingWindow(request.ScheduledStartUtc);

        Appointment? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var existing = await unitOfWork.AppointmentsRepository
                .GetByBookingRequestKeyHashAsync(hash, transactionCancellationToken);
            if (existing is not null)
            {
                EnsureEquivalent(existing, clientPet.Id, request, endUtc, notes, phone);
                result = existing;
                return;
            }

            var locked = await unitOfWork.AvailabilitiesRepository.LockByIdAsync(
                availability.Id,
                transactionCancellationToken)
                ?? throw new ConflictException("La disponibilidad seleccionada ya no existe.");
            existing = await unitOfWork.AppointmentsRepository
                .GetByBookingRequestKeyHashAsync(hash, transactionCancellationToken);
            if (existing is not null)
            {
                EnsureEquivalent(existing, clientPet.Id, request, endUtc, notes, phone);
                result = existing;
                return;
            }

            EnsureAvailabilityMatches(
                locked,
                request.VeterinarianId,
                request.ScheduledStartUtc,
                endUtc,
                service.DurationMinutes);
            if (await unitOfWork.AppointmentsRepository.HasScheduledOverlapAsync(
                    clientPet.Id,
                    request.VeterinarianId,
                    request.ScheduledStartUtc,
                    endUtc,
                    transactionCancellationToken))
            {
                throw new ConflictException("El horario seleccionado ya no está disponible.");
            }

            var statuses = await unitOfWork.StatusAppointmentsRepository.GetAllAsync(
                transactionCancellationToken);
            var status = statuses.SingleOrDefault(item =>
                string.Equals(item.Name, "AGENDADA", StringComparison.OrdinalIgnoreCase))
                ?? throw new ConflictException("No está configurado el estado AGENDADA.");
            var appointment = new Appointment(
                clientPet.Id,
                request.VeterinarianId,
                request.ServiceId,
                status.Id,
                locked.Id,
                request.ScheduledStartUtc,
                endUtc,
                notes,
                phone,
                hash);
            await unitOfWork.AppointmentsRepository.AddAsync(
                appointment,
                transactionCancellationToken);
            await unitOfWork.SaveChangesAsync(transactionCancellationToken);
            result = await unitOfWork.AppointmentsRepository.GetByIdAsync(
                appointment.Id,
                transactionCancellationToken) ?? appointment;
        }, cancellationToken);

        return result ?? throw new InvalidOperationException("La cita no produjo un resultado.");
    }

    private async Task<Availability> ResolveAvailabilityAsync(
        Guid veterinarianId,
        DateTime startUtc,
        DateTime endUtc,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        var availabilities = await unitOfWork.AvailabilitiesRepository
            .GetAllByVeterinarianIdAsync(veterinarianId, cancellationToken);
        return availabilities.SingleOrDefault(item =>
            IsAvailabilityMatch(item, veterinarianId, startUtc, endUtc, durationMinutes))
            ?? throw new ConflictException("El horario no corresponde a una disponibilidad activa.");
    }

    private void EnsureAvailabilityMatches(
        Availability availability,
        Guid veterinarianId,
        DateTime startUtc,
        DateTime endUtc,
        int durationMinutes)
    {
        if (!IsAvailabilityMatch(
                availability, veterinarianId, startUtc, endUtc, durationMinutes))
        {
            throw new ConflictException("La disponibilidad seleccionada cambió.");
        }
    }

    private bool IsAvailabilityMatch(
        Availability availability,
        Guid veterinarianId,
        DateTime startUtc,
        DateTime endUtc,
        int durationMinutes)
    {
        if (!availability.IsActive || availability.VeterinarianId != veterinarianId)
        {
            return false;
        }
        var zone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(startUtc, zone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endUtc, zone);
        var startTime = TimeOnly.FromDateTime(localStart);
        var endTime = TimeOnly.FromDateTime(localEnd);
        var offset = localStart - localStart.Date.Add(availability.StartTime.ToTimeSpan());
        return availability.DayOfWeek == localStart.DayOfWeek
            && startTime >= availability.StartTime
            && endTime <= availability.EndTime
            && offset >= TimeSpan.Zero
            && offset.Ticks % TimeSpan.FromMinutes(durationMinutes).Ticks == 0;
    }

    private void ValidateBookingWindow(DateTime startUtc)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone));
        var requestedDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, zone));
        if (startUtc < nowUtc.Add(settings.MinimumLeadTime)
            || requestedDate > today.AddDays(settings.MaximumAdvanceDays))
        {
            throw new BadRequestException("La fecha está fuera del horizonte de agendamiento.");
        }
    }

    private static string HashKey(Guid userAccountId, string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userAccountId:N}:{key}"));
        return Convert.ToHexString(bytes);
    }

    private static void EnsureEquivalent(
        Appointment existing,
        Guid clientPetId,
        CreateMyAppointmentCommand request,
        DateTime endUtc,
        string? notes,
        string phone)
    {
        if (existing.ClientPetId != clientPetId
            || existing.VeterinarianId != request.VeterinarianId
            || existing.ServiceId != request.ServiceId
            || existing.ScheduledStart != request.ScheduledStartUtc
            || existing.ScheduledEnd != endUtc
            || !string.Equals(existing.Notes, notes, StringComparison.Ordinal)
            || existing.RequesterPhoneNumber?.Matches(phone) != true)
        {
            throw new ConflictException("La clave de idempotencia ya se usó con otros datos.");
        }
    }
}
