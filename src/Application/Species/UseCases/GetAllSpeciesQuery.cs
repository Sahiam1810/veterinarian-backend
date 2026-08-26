using Application.Common.Abstractions;
using Domain.Species.Entities;
using MediatR;

namespace Application.Species.UseCases;

public sealed record GetAllSpeciesQuery() : IRequest<IReadOnlyCollection<SpeciesEntity>>;

public sealed class GetAllSpeciesQueryHandler : IRequestHandler<GetAllSpeciesQuery, IReadOnlyCollection<SpeciesEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllSpeciesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<SpeciesEntity>> Handle(GetAllSpeciesQuery request, CancellationToken cancellationToken)
    {
        return await _uow.SpeciesRepository.GetAllAsync(cancellationToken);
    }
}
