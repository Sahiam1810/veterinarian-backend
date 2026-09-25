using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Pets.ValueObjects;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class QuickBookingAppointmentCommandHandler(
    IUnitOfWork unitOfWork,
    IVeterinarianAbsenceRepository absences,
    TimeProvider timeProvider)
    : IRequestHandler<QuickBookingAppointmentCommand, Guid>
{
    public async Task<Guid> Handle(
        QuickBookingAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. S36: Validar fecha no pasada
        AppointmentPastDateGuard.EnsureNotInThePast(request.ScheduledStart, timeProvider);

        // 2. Validar servicio disponible
        var service = await unitOfWork.ServicesRepository.GetByIdAsync(
            request.ServiceId,
            cancellationToken)
            ?? throw new NotFoundException("Servicio no encontrado.");

        if (!service.IsActive)
        {
            throw new BadRequestException("El servicio no está disponible para citas nuevas.");
        }

        var scheduledEnd = request.ScheduledEnd ?? request.ScheduledStart.AddMinutes(service.DurationMinutes);

        Guid appointmentId = default;

        // 3. Ejecutar todo en una sola transacción atómica
        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            // a. Resolver o crear cliente
            ClientEntity client;
            if (request.ClientId.HasValue && request.ClientId.Value != Guid.Empty)
            {
                client = await unitOfWork.ClientsRepository.GetByIdAsync(
                    request.ClientId.Value,
                    transactionCancellationToken)
                    ?? throw new NotFoundException("Cliente no encontrado.");
            }
            else
            {
                client = await unitOfWork.ClientsRepository.GetByPhoneAsync(
                    request.ClientPhoneNumber!,
                    transactionCancellationToken) ?? null!;

                if (client is null)
                {
                    client = new ClientEntity(
                        fullName: request.ClientFullName!,
                        email: null,
                        identificationNumber: null,
                        phoneNumber: request.ClientPhoneNumber!,
                        address: null);

                    await unitOfWork.ClientsRepository.AddAsync(client, transactionCancellationToken);
                }
            }

            // b. Resolver especie
            var species = await unitOfWork.SpeciesRepository.GetByIdAsync(
                request.SpeciesId,
                transactionCancellationToken)
                ?? throw new NotFoundException("Especie no encontrada.");

            // c. Crear mascota sin datos inventados (edad, peso y raza nulos)
            var pet = new PetEntity(
                name: request.PetName,
                age: null,
                gender: PetGender.Unspecified,
                weight: null,
                observations: null,
                speciesEntity: species,
                raceEntity: null);

            await unitOfWork.PetsRepository.AddAsync(pet, transactionCancellationToken);

            // d. Crear relación cliente - mascota
            var clientPet = new ClientPetEntity(client, pet, isPrimaryOwner: true);
            await unitOfWork.ClientPetsRepository.AddAsync(clientPet, transactionCancellationToken);

            // e. Resolver disponibilidad
            Guid availabilityId;
            if (request.AvailabilityId.HasValue && request.AvailabilityId.Value != Guid.Empty)
            {
                availabilityId = request.AvailabilityId.Value;
            }
            else
            {
                var availabilities = await unitOfWork.AvailabilitiesRepository
                    .GetAllByVeterinarianIdAsync(request.VeterinarianId, transactionCancellationToken);

                TimeZoneInfo zone;
                try
                {
                    zone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
                }
                catch (TimeZoneNotFoundException)
                {
                    zone = TimeZoneInfo.Utc;
                }

                var localStart = request.ScheduledStart.Kind == DateTimeKind.Utc ? TimeZoneInfo.ConvertTimeFromUtc(request.ScheduledStart, zone) : request.ScheduledStart;
                var localEnd = scheduledEnd.Kind == DateTimeKind.Utc ? TimeZoneInfo.ConvertTimeFromUtc(scheduledEnd, zone) : scheduledEnd;

                var startTimeSearch = TimeOnly.FromDateTime(localStart);
                var endTimeSearch = TimeOnly.FromDateTime(localEnd);

                var matchingAvailability = availabilities.FirstOrDefault(a =>
                    a.IsActive &&
                    a.VeterinarianId == request.VeterinarianId &&
                    a.DayOfWeek == localStart.DayOfWeek &&
                    a.StartTime <= startTimeSearch &&
                    a.EndTime >= endTimeSearch);

                if (matchingAvailability is null)
                {
                    matchingAvailability = availabilities.FirstOrDefault(a => a.IsActive && a.VeterinarianId == request.VeterinarianId)
                        ?? throw new ConflictException("No se encontró una disponibilidad activa para el veterinario en la fecha y hora seleccionadas.");
                }

                availabilityId = matchingAvailability.Id;
            }

            // f. Bloquear franja y verificar disponibilidad de agenda
            var locked = await AppointmentSchedulingConcurrency.LockAndEnsureAvailableAsync(
                unitOfWork,
                absences,
                availabilityId,
                clientPet.Id,
                request.VeterinarianId,
                request.ScheduledStart,
                scheduledEnd,
                excludeAppointmentId: null,
                request.ConsultingRoom,
                transactionCancellationToken);

            // g. Obtener estado AGENDADA
            var statuses = await unitOfWork.StatusAppointmentsRepository.GetAllAsync(
                transactionCancellationToken);
            var status = statuses.SingleOrDefault(s =>
                string.Equals(s.Name, AppointmentStatusNames.Agendada, StringComparison.OrdinalIgnoreCase))
                ?? throw new ConflictException("No está configurado el estado AGENDADA.");

            // h. Crear cita médica
            var appointment = new Appointment(
                clientPet.Id,
                request.VeterinarianId,
                request.ServiceId,
                status.Id,
                locked.Id,
                request.ScheduledStart,
                scheduledEnd,
                request.Notes,
                client.PhoneNumber.Value,
                consultingRoom: request.ConsultingRoom ?? locked.ConsultingRoom);

            await unitOfWork.AppointmentsRepository.AddAsync(appointment, transactionCancellationToken);
            appointmentId = appointment.Id;

            await unitOfWork.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);

        return appointmentId;
    }
}
