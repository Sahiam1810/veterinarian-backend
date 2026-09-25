using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Pets.Models;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using MediatR;

namespace Application.Pets.UseCases;

public sealed record RegisterMyPetCommand(
    Guid ClientId,
    string Name,
    int Age,
    string Gender,
    decimal Weight,
    string? Observations,
    Guid SpeciesId,
    Guid RaceId,
    string? PhotoUrl = null) : IRequest<OwnedPetProfile>;

public sealed class RegisterMyPetCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterMyPetCommand, OwnedPetProfile>
{
    public async Task<OwnedPetProfile> Handle(
        RegisterMyPetCommand request,
        CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientsRepository.GetByIdAsync(
            request.ClientId, cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");

        var species = await unitOfWork.SpeciesRepository.GetByIdAsync(
            request.SpeciesId, cancellationToken)
            ?? throw new NotFoundException("Especie no encontrada.");

        var race = await unitOfWork.RacesRepository.GetByIdAsync(
            request.RaceId, cancellationToken)
            ?? throw new NotFoundException("Raza no encontrada.");

        var pet = new PetEntity(
            request.Name,
            request.Age,
            request.Gender,
            request.Weight,
            request.Observations,
            species,
            race,
            request.PhotoUrl);
        var ownership = new ClientPetEntity(client, pet, isPrimaryOwner: true);

        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            await unitOfWork.PetsRepository.AddAsync(pet, transactionToken);
            await unitOfWork.ClientPetsRepository.AddAsync(ownership, transactionToken);
        }, cancellationToken);

        return new OwnedPetProfile(
            pet.Id,
            pet.Name.Value,
            pet.Age,
            pet.Gender.Value,
            pet.Weight.Value,
            pet.Observations.Value,
            pet.SpeciesId,
            species.Name.Value,
            pet.RaceId,
            race.Name.Value,
            pet.UpdatedAt ?? pet.CreatedAt,
            pet.PhotoUrl.Value);
    }
}
