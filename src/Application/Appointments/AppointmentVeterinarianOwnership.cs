using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;

namespace Application.Appointments;

internal static class AppointmentVeterinarianOwnership
{
    public static async Task EnsureAsync(
        IUnitOfWork unitOfWork,
        Appointment appointment,
        Guid actorUserId,
        bool enforceVeterinarianOwnership,
        CancellationToken cancellationToken)
    {
        if (!enforceVeterinarianOwnership)
        {
            return;
        }

        // U4: el sub ya es el id del usuario; ya no hace falta pasar por UserAccounts.
        var veterinarian = await unitOfWork.VeterinariansRepository.GetByUserIdAsync(
            actorUserId,
            cancellationToken)
            ?? throw new NotFoundException(
                "El usuario autenticado no tiene un perfil de veterinario asociado.");

        if (appointment.VeterinarianId != veterinarian.Id)
        {
            throw new ForbiddenException(
                "La cita no está asignada al veterinario autenticado.");
        }
    }
}
