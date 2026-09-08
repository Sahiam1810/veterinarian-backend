using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Races.Entities;
using MediatR;

namespace Application.Races.UseCases;

public sealed record CreateRaceCommand(string Name, Guid SpeciesId) : IRequest<Guid>;

public sealed class CreateRaceCommandHandler : IRequestHandler<CreateRaceCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateRaceCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateRaceCommand request, CancellationToken cancellationToken)
    {
        var species = await _uow.SpeciesRepository.GetByIdAsync(request.SpeciesId, cancellationToken);
        if (species is null)
        {
            throw new NotFoundException("Especie no encontrada.");
        }

        var exists = await _uow.RacesRepository.ExistsByNameInSpeciesAsync(
            request.Name,
            request.SpeciesId,
            cancellationToken);
        if (exists)
        {
            throw new ConflictException("Ya existe una raza con ese nombre en la especie.");
        }

        var race = new RaceEntity(request.Name, species);

        await _uow.RacesRepository.AddAsync(race, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return race.Id;
    }
}
