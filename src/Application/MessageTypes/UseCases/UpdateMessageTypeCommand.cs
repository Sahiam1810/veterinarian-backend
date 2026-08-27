using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.MessageTypes.UseCases;

public sealed record UpdateMessageTypeCommand(Guid Id, string Name) : IRequest;

public sealed class UpdateMessageTypeCommandHandler : IRequestHandler<UpdateMessageTypeCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateMessageTypeCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(UpdateMessageTypeCommand request, CancellationToken cancellationToken)
    {
        var messageType = await _uow.MessageTypesRepository.GetByIdAsync(request.Id, cancellationToken);
        if (messageType is null)
            throw new NotFoundException("Tipo de mensaje no encontrado.");

        messageType.Update(request.Name);
        await _uow.MessageTypesRepository.UpdateAsync(messageType, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
