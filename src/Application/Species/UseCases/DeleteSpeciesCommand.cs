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

        await _uow.SpeciesRepository.DeleteAsync(species, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
