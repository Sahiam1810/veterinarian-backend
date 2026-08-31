using Application.Common.Abstractions;
using MediatR;
using ChatAiRunEntity = Domain.ChatAiRuns.Entities.ChatAiRun;

namespace Application.ChatAiRuns.UseCase;

public sealed record GetChatAiRunByIdQuery(Guid Id) : IRequest<ChatAiRunEntity?>;

public sealed class GetChatAiRunByIdQueryHandler
    : IRequestHandler<GetChatAiRunByIdQuery, ChatAiRunEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatAiRunByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatAiRunEntity?> Handle(
        GetChatAiRunByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatAiRunsRepository.GetByIdAsync(request.Id, cancellationToken);
}

public sealed record GetChatAiRunsByConversationIdQuery(Guid ChatConversationId)
    : IRequest<IReadOnlyCollection<ChatAiRunEntity>>;

public sealed class GetChatAiRunsByConversationIdQueryHandler
    : IRequestHandler<GetChatAiRunsByConversationIdQuery, IReadOnlyCollection<ChatAiRunEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatAiRunsByConversationIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatAiRunEntity>> Handle(
        GetChatAiRunsByConversationIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatAiRunsRepository.GetAllByConversationIdAsync(
            request.ChatConversationId,
            cancellationToken);
}
