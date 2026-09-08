using Application.Common.Abstractions;
using Domain.Races.Entities;
using MediatR;

namespace Application.Races.UseCases;

// speciesId opcional: si viene, solo razas de esa especie
public sealed record GetAllRacesQuery(Guid? SpeciesId = null)
    : IRequest<IReadOnlyCollection<RaceEntity>>;

public sealed class GetAllRacesQueryHandler
    : IRequestHandler<GetAllRacesQuery, IReadOnlyCollection<RaceEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllRacesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<RaceEntity>> Handle(
        GetAllRacesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.SpeciesId is { } speciesId && speciesId != Guid.Empty)
        {
            return await _uow.RacesRepository.GetBySpeciesIdAsync(speciesId, cancellationToken);
        }

        return await _uow.RacesRepository.GetAllAsync(cancellationToken);
    }
}
