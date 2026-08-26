using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Pets.UseCases;

public sealed record UpdatePetCommand(
    Guid Id,
    string Name,
    int Age,
    string Gender,
    decimal Weight,
    string? Observations,
    Guid SpeciesId,
    Guid RaceId) : IRequest;

public sealed class UpdatePetCommandHandler : IRequestHandler<UpdatePetCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdatePetCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(UpdatePetCommand request, CancellationToken cancellationToken)
    {
        var pet = await _uow.PetsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (pet is null)
            throw new NotFoundException("Mascota no encontrada.");

        var species = await _uow.SpeciesRepository.GetByIdAsync(request.SpeciesId, cancellationToken);
        if (species is null)
            throw new NotFoundException("Especie no encontrada.");

        var race = await _uow.RacesRepository.GetByIdAsync(request.RaceId, cancellationToken);
        if (race is null)
            throw new NotFoundException("Raza no encontrada.");

        pet.Update(
            request.Name,
            request.Age,
            request.Gender,
            request.Weight,
            request.Observations,
            species,
            race);

        await _uow.PetsRepository.UpdateAsync(pet, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
