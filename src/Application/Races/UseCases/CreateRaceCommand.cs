using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using veterinarian_backend.Domain.Races.Entities;

namespace Application.Races.UseCases;

public sealed record CreateRaceCommand(string Name) : IRequest<Guid>;

public sealed class CreateRaceCommandHandler : IRequestHandler<CreateRaceCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateRaceCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateRaceCommand request, CancellationToken cancellationToken)
    {
        var exists = await _uow.RacesRepository.ExistsByNameAsync(request.Name, cancellationToken);
        if (exists)
        {
            throw new ConflictException("Ya existe una raza con ese nombre.");
        }

        var race = new RaceEntity 
        { 
            Id = Guid.NewGuid(), 
            Name = request.Name 
        };

        await _uow.RacesRepository.AddAsync(race, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return race.Id;
    }
}
