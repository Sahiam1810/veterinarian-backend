using Application.Common.Abstractions;
using Application.Common.Exceptions;

namespace Application.Appointments.UseCases;

internal static class AppointmentSchedulingConcurrency
{
    public static async Task LockAndEnsureAvailableAsync(
        IUnitOfWork unitOfWork,
        Guid availabilityId,
        Guid clientPetId,
        Guid veterinarianId,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        var availability = await unitOfWork.AvailabilitiesRepository.LockByIdAsync(
            availabilityId,
            cancellationToken)
            ?? throw new ConflictException("La disponibilidad seleccionada ya no existe.");

        if (!availability.IsActive || availability.VeterinarianId != veterinarianId)
        {
            throw new ConflictException("La disponibilidad seleccionada ya no es válida.");
        }

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
}
