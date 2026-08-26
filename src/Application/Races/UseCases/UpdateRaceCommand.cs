using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Races.Entities;
using MediatR;

namespace Application.Races.UseCases;

public sealed record UpdateRaceCommand(Guid Id, string Name) : IRequest;

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

        var exists = await _uow.RacesRepository.ExistsByNameAsync(request.Name, cancellationToken, request.Id);
        if (exists)
        {
            throw new ConflictException("Ya existe otra raza con ese nombre.");
        }

        race.Update(request.Name);
        
        await _uow.RacesRepository.UpdateAsync(race, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
