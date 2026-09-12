using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Species.UseCases;

public sealed record DeleteSpeciesCommand(Guid Id) : IRequest;

public sealed class DeleteSpeciesCommandHandler : IRequestHandler<DeleteSpeciesCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteSpeciesCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(DeleteSpeciesCommand request, CancellationToken cancellationToken)
    {
        var species = await _uow.SpeciesRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (species is null)
        {
            throw new NotFoundException("Especie no encontrada.");
        }

        // No borrar si aún hay razas (Restrict en BD)
        var races = await _uow.RacesRepository.GetAllAsync(species.Id, cancellationToken);
        if (races.Count > 0)
        {
            throw new ConflictException(
                "La especie tiene razas asociadas. Elimina las razas primero.");
        }

        await _uow.SpeciesRepository.DeleteAsync(species, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
