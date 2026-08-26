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

        await _uow.ClientsRepository.DeleteAsync(client, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
