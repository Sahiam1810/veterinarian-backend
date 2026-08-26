using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Species.Entities;
using MediatR;

namespace Application.Species.UseCases;

public sealed record CreateSpeciesCommand(string Name) : IRequest<Guid>;

public sealed class CreateSpeciesCommandHandler : IRequestHandler<CreateSpeciesCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateSpeciesCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateSpeciesCommand request, CancellationToken cancellationToken)
    {
        var exists = await _uow.SpeciesRepository.ExistsByNameAsync(request.Name, cancellationToken);
        if (exists)
        {
            throw new ConflictException("Ya existe una especie con ese nombre.");
        }

        var species = new SpeciesEntity(request.Name);

        await _uow.SpeciesRepository.AddAsync(species, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return species.Id;
    }
}
