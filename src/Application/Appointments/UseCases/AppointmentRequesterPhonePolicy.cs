using Application.Common.Abstractions;
using Application.Common.Exceptions;

namespace Application.Appointments.UseCases;

// Fuente de verdad del telefono de cita: perfil del dueño si existe; si no, el request.
public static class AppointmentRequesterPhonePolicy
{
    // resolve: perfil gana e ignora request divergente; sin perfil usa request.
    // requirePhone: en Create exige un valor; en Update puede devolver null (conservar).
    public static async Task<string?> ResolveAsync(
        IUnitOfWork unitOfWork,
        Guid clientPetId,
        string? requestPhoneNumber,
        bool requirePhone,
        CancellationToken cancellationToken)
    {
        var clientPet = await unitOfWork.ClientPetsRepository.GetByIdAsync(
            clientPetId,
            cancellationToken)
            ?? throw new NotFoundException("Relacion cliente-mascota no encontrada.");

        var client = await unitOfWork.ClientsRepository.GetByIdAsync(
            clientPet.ClientId,
            cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");

        if (!string.IsNullOrWhiteSpace(client.PhoneNumber?.Value))
        {
            // Perfil manda: se copia el valor ya normalizado del dueño.
            return client.PhoneNumber.Value;
        }

        if (!string.IsNullOrWhiteSpace(requestPhoneNumber))
        {
            return requestPhoneNumber.Trim();
        }

        if (requirePhone)
        {
            throw new BadRequestException("El telefono del solicitante es requerido.");
        }

        return null;
    }
}