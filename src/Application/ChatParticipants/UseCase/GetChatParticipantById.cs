using Application.Common.Abstractions;
using MediatR;
using ChatParticipantEntity = Domain.ChatParticipants.Entities.ChatParticipant;

namespace Application.ChatParticipants.UseCase;

public sealed record GetChatParticipantByIdQuery(Guid Id)
    : IRequest<ChatParticipantEntity?>;

public sealed class GetChatParticipantByIdQueryHandler
    : IRequestHandler<GetChatParticipantByIdQuery, ChatParticipantEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatParticipantByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatParticipantEntity?> Handle(
        GetChatParticipantByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatParticipantsRepository.GetByIdAsync(request.Id, cancellationToken);
}
