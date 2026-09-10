using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.VeterinarianAbsences.Abstraction;
using Domain.Availabilities.Entities;

namespace Application.Appointments.UseCases;

internal static class AppointmentSchedulingConcurrency
{
    public static async Task<Availability> LockAvailabilityAsync(
        IUnitOfWork unitOfWork,
        Guid availabilityId,
        CancellationToken cancellationToken)
        => await unitOfWork.AvailabilitiesRepository.LockByIdAsync(
            availabilityId,
            cancellationToken)
            ?? throw new ConflictException("La disponibilidad seleccionada ya no existe.");

    public static async Task<Availability> LockAndEnsureAvailableAsync(
        IUnitOfWork unitOfWork,
        IVeterinarianAbsenceRepository absences,
        Guid availabilityId,
        Guid clientPetId,
        Guid veterinarianId,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        Guid? excludeAppointmentId,
        string? consultingRoom,
        CancellationToken cancellationToken)
    {
        var availability = await LockAvailabilityAsync(
            unitOfWork,
            availabilityId,
            cancellationToken);

        await EnsureAvailableAsync(
            unitOfWork,
            absences,
            availability,
            clientPetId,
            veterinarianId,
            scheduledStart,
            scheduledEnd,
            excludeAppointmentId,
            consultingRoom,
            cancellationToken);

        return availability;
    }

    public static async Task EnsureAvailableAsync(
        IUnitOfWork unitOfWork,
        IVeterinarianAbsenceRepository absences,
        Availability availability,
        Guid clientPetId,
        Guid veterinarianId,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        Guid? excludeAppointmentId,
        string? consultingRoom,
        CancellationToken cancellationToken)
    {
        if (!availability.IsActive || availability.VeterinarianId != veterinarianId)
        {
            throw new ConflictException("La disponibilidad seleccionada ya no es valida.");
        }

        var overlappingAbsences = await absences.GetOverlappingAsync(
            veterinarianId,
            scheduledStart,
            scheduledEnd,
            cancellationToken);
        if (overlappingAbsences.Any(item => item.Overlaps(scheduledStart, scheduledEnd)))
        {
            throw new ConflictException(
                "El veterinario tiene una ausencia en el horario seleccionado.");
        }

        if (availability.MaxConcurrentAppointments <= 1)
        {
            if (await unitOfWork.AppointmentsRepository.HasOverlappingAppointmentAsync(
                    clientPetId,
                    veterinarianId,
                    scheduledStart,
                    scheduledEnd,
                    excludeAppointmentId,
                    cancellationToken))
            {
                throw new ConflictException(
                    "Ya existe una cita para la mascota o el veterinario en el horario seleccionado.");
            }
        }
        else
        {
            if (await unitOfWork.AppointmentsRepository.HasClientPetOverlapAsync(
                    clientPetId,
                    scheduledStart,
                    scheduledEnd,
                    excludeAppointmentId,
                    cancellationToken))
            {
                throw new ConflictException(
                    "Ya existe una cita para la mascota en el horario seleccionado.");
            }

            var occupancy = await unitOfWork.AppointmentsRepository
                .CountScheduledOverlapsForVeterinarianAsync(
                    veterinarianId,
                    scheduledStart,
                    scheduledEnd,
                    excludeAppointmentId,
                    cancellationToken);
            if (occupancy >= availability.MaxConcurrentAppointments)
            {
                throw new ConflictException(
                    "El veterinario ya alcanzo el maximo de citas concurrentes en ese horario.");
            }
        }

        var room = string.IsNullOrWhiteSpace(consultingRoom)
            ? availability.ConsultingRoom
            : consultingRoom;
        if (!string.IsNullOrWhiteSpace(room)
            && await unitOfWork.AppointmentsRepository.HasConsultingRoomOverlapAsync(
                room,
                scheduledStart,
                scheduledEnd,
                excludeAppointmentId,
                cancellationToken))
        {
            throw new ConflictException(
                "El consultorio ya esta ocupado en el horario seleccionado.");
        }
    }
}
