using Application.Common.Abstractions;
using MediatR;
using veterinarian_backend.Domain.Races.Entities;

namespace Application.Races.UseCases;

public sealed record GetAllRacesQuery() : IRequest<IReadOnlyCollection<RaceEntity>>;

public sealed class GetAllRacesQueryHandler : IRequestHandler<GetAllRacesQuery, IReadOnlyCollection<RaceEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllRacesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<RaceEntity>> Handle(GetAllRacesQuery request, CancellationToken cancellationToken)
    {
        return await _uow.RacesRepository.GetAllAsync(cancellationToken);
    }
}
