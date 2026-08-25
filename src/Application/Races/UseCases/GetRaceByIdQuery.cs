using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using veterinarian_backend.Domain.Races.Entities;

namespace Application.Races.UseCases;

public sealed record GetRaceByIdQuery(Guid Id) : IRequest<RaceEntity>;

public sealed class GetRaceByIdQueryHandler : IRequestHandler<GetRaceByIdQuery, RaceEntity>
{
    private readonly IUnitOfWork _uow;

    public GetRaceByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<RaceEntity> Handle(GetRaceByIdQuery request, CancellationToken cancellationToken)
    {
        var race = await _uow.RacesRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (race is null)
        {
            throw new NotFoundException("Raza no encontrada.");
        }
        
        return race;
    }
}
