using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Clients.UseCases;

public sealed record DeleteClientCommand(Guid Id) : IRequest;

public sealed class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteClientCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        var client = await _uow.ClientsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (client is null)
        {
            throw new NotFoundException("Cliente no encontrado.");
        }

        // CLIENTS_PETS.CLIENT_ID -> CLIENTS.ID es RESTRICT: sin este chequeo, el borrado
        // revienta con una violación de integridad genérica en vez de un mensaje claro.
        var linkedPets = await _uow.ClientPetsRepository.GetByClientIdAsync(request.Id, cancellationToken);
        if (linkedPets.Count > 0)
        {
            throw new ConflictException(
                "Este dueño tiene mascotas asociadas. Debes eliminar o reasignar sus mascotas antes de eliminarlo.");
        }

        await _uow.ClientsRepository.DeleteAsync(client, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
