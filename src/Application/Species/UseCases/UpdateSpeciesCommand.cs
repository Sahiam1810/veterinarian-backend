using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using veterinarian_backend.Domain.Species.Entities;

namespace Application.Species.UseCases;

public sealed record UpdateSpeciesCommand(Guid Id, string Name) : IRequest;

public sealed class UpdateSpeciesCommandHandler : IRequestHandler<UpdateSpeciesCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateSpeciesCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(UpdateSpeciesCommand request, CancellationToken cancellationToken)
    {
        var species = await _uow.SpeciesRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (species is null)
        {
            throw new NotFoundException("Especie no encontrada.");
        }

        var exists = await _uow.SpeciesRepository.ExistsByNameAsync(request.Name, cancellationToken, request.Id);
        if (exists)
        {
            throw new ConflictException("Ya existe otra especie con ese nombre.");
        }

        species.Name = request.Name;
        
        await _uow.SpeciesRepository.UpdateAsync(species, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
