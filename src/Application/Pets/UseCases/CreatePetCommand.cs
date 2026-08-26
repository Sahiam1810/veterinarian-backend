using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Pets.Entities;
using MediatR;

namespace Application.Pets.UseCases;

public sealed record CreatePetCommand(
    string Name,
    int Age,
    string Gender,
    decimal Weight,
    string? Observations,
    Guid SpeciesId,
    Guid RaceId) : IRequest<Guid>;

public sealed class CreatePetCommandHandler : IRequestHandler<CreatePetCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreatePetCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreatePetCommand request, CancellationToken cancellationToken)
    {
        var species = await _uow.SpeciesRepository.GetByIdAsync(request.SpeciesId, cancellationToken);
        if (species is null)
            throw new NotFoundException("Especie no encontrada.");

        var race = await _uow.RacesRepository.GetByIdAsync(request.RaceId, cancellationToken);
        if (race is null)
            throw new NotFoundException("Raza no encontrada.");

        var pet = new PetEntity(
            request.Name,
            request.Age,
            request.Gender,
            request.Weight,
            request.Observations,
            species,
            race);

        await _uow.PetsRepository.AddAsync(pet, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return pet.Id;
    }
}
