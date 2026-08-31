using Application.Common.Abstractions;
using MediatR;
using ChatAiRunErrorEntity = Domain.ChatAiRunErrors.Entities.ChatAiRunError;

namespace Application.ChatAiRunErrors.UseCase;

public sealed record GetChatAiRunErrorByIdQuery(Guid Id) : IRequest<ChatAiRunErrorEntity?>;

public sealed class GetChatAiRunErrorByIdQueryHandler
    : IRequestHandler<GetChatAiRunErrorByIdQuery, ChatAiRunErrorEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatAiRunErrorByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatAiRunErrorEntity?> Handle(
        GetChatAiRunErrorByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatAiRunErrorsRepository.GetByIdAsync(request.Id, cancellationToken);
}

public sealed record GetChatAiRunErrorsByChatAiRunIdQuery(Guid ChatAiRunId)
    : IRequest<IReadOnlyCollection<ChatAiRunErrorEntity>>;

public sealed class GetChatAiRunErrorsByChatAiRunIdQueryHandler
    : IRequestHandler<GetChatAiRunErrorsByChatAiRunIdQuery, IReadOnlyCollection<ChatAiRunErrorEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatAiRunErrorsByChatAiRunIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatAiRunErrorEntity>> Handle(
        GetChatAiRunErrorsByChatAiRunIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatAiRunErrorsRepository.GetAllByChatAiRunIdAsync(
            request.ChatAiRunId,
            cancellationToken);
}
