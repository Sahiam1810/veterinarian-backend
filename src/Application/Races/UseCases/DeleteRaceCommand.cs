using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Races.UseCases;

public sealed record DeleteRaceCommand(Guid Id) : IRequest;

public sealed class DeleteRaceCommandHandler : IRequestHandler<DeleteRaceCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteRaceCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(DeleteRaceCommand request, CancellationToken cancellationToken)
    {
        var race = await _uow.RacesRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (race is null)
        {
            throw new NotFoundException("Raza no encontrada.");
        }

        await _uow.RacesRepository.DeleteAsync(race, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
