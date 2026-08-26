using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Species.Entities;
using MediatR;

namespace Application.Species.UseCases;

public sealed record GetSpeciesByIdQuery(Guid Id) : IRequest<SpeciesEntity>;

public sealed class GetSpeciesByIdQueryHandler : IRequestHandler<GetSpeciesByIdQuery, SpeciesEntity>
{
    private readonly IUnitOfWork _uow;

    public GetSpeciesByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<SpeciesEntity> Handle(GetSpeciesByIdQuery request, CancellationToken cancellationToken)
    {
        var species = await _uow.SpeciesRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (species is null)
        {
            throw new NotFoundException("Especie no encontrada.");
        }
        
        return species;
    }
}
