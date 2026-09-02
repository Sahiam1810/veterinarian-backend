using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;

namespace Application.Appointments;

internal static class AppointmentVeterinarianOwnership
{
    public static async Task EnsureAsync(
        IUnitOfWork unitOfWork,
        Appointment appointment,
        Guid actorUserAccountId,
        bool enforceVeterinarianOwnership,
        CancellationToken cancellationToken)
    {
        if (!enforceVeterinarianOwnership)
        {
            return;
        }

        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
            actorUserAccountId,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var veterinarian = await unitOfWork.VeterinariansRepository.GetByUserIdAsync(
            account.UserId,
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
