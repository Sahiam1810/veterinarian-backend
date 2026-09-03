using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record AppointmentBookingPet(Guid Id, string Name);

public sealed record AppointmentBookingService(Guid Id, string Name, int DurationMinutes);

public sealed record AppointmentBookingVeterinarian(
    Guid Id,
    string FullName,
    string SpecialtyName);

public sealed record AppointmentBookingOptionsResult(
    IReadOnlyCollection<AppointmentBookingPet> Pets,
    IReadOnlyCollection<AppointmentBookingService> Services,
    IReadOnlyCollection<AppointmentBookingVeterinarian> Veterinarians,
    bool RequiresRequesterPhoneNumber);

public sealed record GetAppointmentBookingOptionsQuery(Guid UserAccountId)
    : IRequest<AppointmentBookingOptionsResult>;

public sealed class GetAppointmentBookingOptionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAppointmentBookingOptionsQuery, AppointmentBookingOptionsResult>
{
    public async Task<AppointmentBookingOptionsResult> Handle(
        GetAppointmentBookingOptionsQuery request,
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

        var ownerships = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(
            client.Id,
            cancellationToken);
        var pets = await unitOfWork.PetsRepository.GetByIdsAsync(
            ownerships.Select(item => item.PetId).Distinct().ToArray(),
            cancellationToken);
        var services = await unitOfWork.ServicesRepository.GetAvailableAsync(cancellationToken);
        var veterinarians = await unitOfWork.VeterinariansRepository.GetAllAsync(cancellationToken);

        return new AppointmentBookingOptionsResult(
            pets.Select(pet => new AppointmentBookingPet(pet.Id, pet.Name.Value))
                .OrderBy(pet => pet.Name)
                .ToArray(),
            services.Where(service => service.IsActive)
                .Select(service => new AppointmentBookingService(
                    service.Id,
                    service.Name,
                    service.DurationMinutes))
                .OrderBy(service => service.Name)
                .ToArray(),
            veterinarians
                .Where(veterinarian => veterinarian.User?.IsActive == true)
                .Select(veterinarian => new AppointmentBookingVeterinarian(
                    veterinarian.Id,
                    veterinarian.User!.FullName,
                    veterinarian.Specialty?.Name.Value ?? "General"))
                .OrderBy(veterinarian => veterinarian.FullName)
                .ToArray(),
            client.PhoneNumber is null);
    }
}
