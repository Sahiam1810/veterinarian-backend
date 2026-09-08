using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Races.UseCases;

public sealed record UpdateRaceCommand(Guid Id, string Name, Guid SpeciesId) : IRequest;

public sealed class UpdateRaceCommandHandler : IRequestHandler<UpdateRaceCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateRaceCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(UpdateRaceCommand request, CancellationToken cancellationToken)
    {
        var race = await _uow.RacesRepository.GetByIdAsync(request.Id, cancellationToken);
        if (race is null)
        {
            throw new NotFoundException("Raza no encontrada.");
        }

        var species = await _uow.SpeciesRepository.GetByIdAsync(request.SpeciesId, cancellationToken);
        if (species is null)
        {
            throw new NotFoundException("Especie no encontrada.");
        }

        var exists = await _uow.RacesRepository.ExistsByNameInSpeciesAsync(
            request.Name,
            request.SpeciesId,
            cancellationToken,
            request.Id);
        if (exists)
        {
            throw new ConflictException("Ya existe otra raza con ese nombre en la especie.");
        }

        race.Update(request.Name, request.SpeciesId);

        await _uow.RacesRepository.UpdateAsync(race, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
