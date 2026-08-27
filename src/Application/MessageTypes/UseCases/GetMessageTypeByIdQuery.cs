using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.MessageTypes.Entities;
using MediatR;

namespace Application.MessageTypes.UseCases;

public sealed record GetMessageTypeByIdQuery(Guid Id) : IRequest<MessageTypeEntity>;

public sealed class GetMessageTypeByIdQueryHandler : IRequestHandler<GetMessageTypeByIdQuery, MessageTypeEntity>
{
    private readonly IUnitOfWork _uow;

    public GetMessageTypeByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<MessageTypeEntity> Handle(GetMessageTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var messageType = await _uow.MessageTypesRepository.GetByIdAsync(request.Id, cancellationToken);

        if (messageType is null)
            throw new NotFoundException("Tipo de mensaje no encontrado.");

        return messageType;
    }
}
