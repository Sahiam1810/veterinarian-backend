using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.MessageTypes.UseCases;

public sealed record DeleteMessageTypeCommand(Guid Id) : IRequest;

public sealed class DeleteMessageTypeCommandHandler : IRequestHandler<DeleteMessageTypeCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteMessageTypeCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(DeleteMessageTypeCommand request, CancellationToken cancellationToken)
    {
        var messageType = await _uow.MessageTypesRepository.GetByIdAsync(request.Id, cancellationToken);

        if (messageType is null)
            throw new NotFoundException("Tipo de mensaje no encontrado.");

        await _uow.MessageTypesRepository.DeleteAsync(messageType, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
