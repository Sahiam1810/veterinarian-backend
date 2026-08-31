using Application.Common.Abstractions;
using MediatR;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.ChatEscalations.UseCase;

public sealed record GetChatEscalationByIdQuery(Guid Id)
    : IRequest<ChatEscalationEntity?>;

public sealed class GetChatEscalationByIdQueryHandler
    : IRequestHandler<GetChatEscalationByIdQuery, ChatEscalationEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatEscalationByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatEscalationEntity?> Handle(
        GetChatEscalationByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationsRepository.GetByIdAsync(request.Id, cancellationToken);
}
