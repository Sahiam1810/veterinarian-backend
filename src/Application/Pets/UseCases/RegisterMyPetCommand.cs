using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Pets.Models;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using MediatR;

namespace Application.Pets.UseCases;

public sealed record RegisterMyPetCommand(
    Guid UserAccountId,
    string Name,
    int Age,
    string Gender,
    decimal Weight,
    string? Observations,
    Guid SpeciesId,
    Guid RaceId) : IRequest<OwnedPetProfile>;

public sealed class RegisterMyPetCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterMyPetCommand, OwnedPetProfile>
{
    public async Task<OwnedPetProfile> Handle(
        RegisterMyPetCommand request,
        CancellationToken cancellationToken)
    {
        var account = await unitOfWork.UserAccountsRepository.GetByIdAsync(
            request.UserAccountId, cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");

        var client = await unitOfWork.ClientsRepository.GetByUserIdAsync(
            account.UserId, cancellationToken)
            ?? throw new NotFoundException(
                "El usuario autenticado no tiene un perfil de cliente asociado.");

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
            race);
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
            pet.UpdatedAt ?? pet.CreatedAt);
    }
}
