using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.HospitalizationStays.Entities;
using Domain.MedicalOrders;

namespace Application.MedicalOrders;

// Valida el origen de una orden médica (cita o estancia) y decide qué veterinario la firma.
// - Cita: el veterinario de la cita (comportamiento original, sin cambios).
// - Estancia: el veterinario autenticado que la receta; si no es veterinario → 403.
internal static class MedicalOrderOriginResolver
{
    public static async Task<Guid> ResolveVeterinarianIdAsync(
        IUnitOfWork unitOfWork,
        Guid clientPetId,
        Guid? appointmentId,
        Guid? hospitalizationStayId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (!MedicalOrderOriginRules.HasExactlyOneOrigin(appointmentId, hospitalizationStayId))
        {
            throw new BadRequestException(MedicalOrderOriginRules.ExactlyOneOriginMessage);
        }

        if (MedicalOrderOriginRules.HasValue(appointmentId))
        {
            var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
                appointmentId!.Value,
                cancellationToken)
                ?? throw new NotFoundException($"No se encontró la cita con ID '{appointmentId}'.");

            return appointment.VeterinarianId;
        }

        var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(
            hospitalizationStayId!.Value,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la estancia de hospitalización con ID '{hospitalizationStayId}'.");

        if (stay.Estado != HospitalizationStayStatus.Activa)
        {
            throw new ConflictException("No se pueden crear órdenes en una estancia dada de alta.");
        }

        if (stay.ClientPetId != clientPetId)
        {
            throw new BadRequestException("La mascota de la orden no coincide con la de la estancia.");
        }

        var veterinarian = await unitOfWork.VeterinariansRepository.GetByUserIdAsync(
            actorUserId,
            cancellationToken)
            ?? throw new ForbiddenException("Solo un veterinario puede crear órdenes de hospitalización.");

        return veterinarian.Id;
    }
}
