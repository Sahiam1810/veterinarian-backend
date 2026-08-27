using Application.Common.Abstractions;
using Domain.MessageTypes.Entities;
using MediatR;

namespace Application.MessageTypes.UseCases;

public sealed record CreateMessageTypeCommand(string Name) : IRequest<Guid>;

public sealed class CreateMessageTypeCommandHandler : IRequestHandler<CreateMessageTypeCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateMessageTypeCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateMessageTypeCommand request, CancellationToken cancellationToken)
    {
        var messageType = new MessageTypeEntity(request.Name);
        await _uow.MessageTypesRepository.AddAsync(messageType, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return messageType.Id;
    }
}
