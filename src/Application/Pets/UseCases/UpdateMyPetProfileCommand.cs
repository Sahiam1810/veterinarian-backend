using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Pets.Models;
using MediatR;

namespace Application.Pets.UseCases;

public sealed record UpdateMyPetProfileCommand(
    Guid UserAccountId,
    Guid PetId,
    string? Name,
    int? Age,
    string? Gender,
    decimal? Weight,
    string? Observations,
    bool ChangeObservations,
    Guid? SpeciesId,
    Guid? RaceId,
    DateTime ExpectedUpdatedAt) : IRequest<OwnedPetProfile>;

public sealed class UpdateMyPetProfileCommandHandler
    : IRequestHandler<UpdateMyPetProfileCommand, OwnedPetProfile>
{
    private readonly IUnitOfWork _uow;

    public UpdateMyPetProfileCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<OwnedPetProfile> Handle(
        UpdateMyPetProfileCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(
            request.UserAccountId, cancellationToken);
        if (account is null)
            throw new NotFoundException("Cuenta de usuario no encontrada.");

        var client = await _uow.ClientsRepository.GetByUserIdAsync(
            account.UserId, cancellationToken);
        if (client is null)
            throw new NotFoundException("El usuario autenticado no tiene un perfil de cliente asociado.");

        var ownsPet = await _uow.ClientPetsRepository.ExistsByClientAndPetAsync(
            client.Id, request.PetId, cancellationToken);
        if (!ownsPet)
            throw new NotFoundException("Mascota no encontrada.");

        var pet = await _uow.PetsRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            throw new NotFoundException("Mascota no encontrada.");

        var currentVersion = pet.UpdatedAt ?? pet.CreatedAt;
        if (AsUtc(currentVersion) != AsUtc(request.ExpectedUpdatedAt))
            throw new ConflictException("El perfil de la mascota cambió; consulta sus datos nuevamente.");

        var speciesId = request.SpeciesId ?? pet.SpeciesId;
        var species = await _uow.SpeciesRepository.GetByIdAsync(speciesId, cancellationToken);
        if (species is null)
            throw new NotFoundException("Especie no encontrada.");

        var raceId = request.RaceId ?? pet.RaceId;
        var race = await _uow.RacesRepository.GetByIdAsync(raceId, cancellationToken);
        if (race is null)
            throw new NotFoundException("Raza no encontrada.");

        pet.Update(
            request.Name ?? pet.Name.Value,
            request.Age ?? pet.Age,
            request.Gender ?? pet.Gender.Value,
            request.Weight ?? pet.Weight.Value,
            request.ChangeObservations ? request.Observations : pet.Observations.Value,
            species,
            race);

        await _uow.PetsRepository.UpdateAsync(pet, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new OwnedPetProfile(
            pet.Id, pet.Name.Value, pet.Age, pet.Gender.Value, pet.Weight.Value,
            pet.Observations.Value, pet.SpeciesId, pet.Species.Name.Value,
            pet.RaceId, pet.Race.Name.Value, pet.UpdatedAt ?? pet.CreatedAt);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
