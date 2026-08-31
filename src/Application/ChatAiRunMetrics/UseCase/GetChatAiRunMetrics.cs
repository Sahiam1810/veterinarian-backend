using Application.Common.Abstractions;
using MediatR;
using ChatAiRunMetricsEntity = Domain.ChatAiRunMetrics.Entities.ChatAiRunMetrics;

namespace Application.ChatAiRunMetrics.UseCase;

public sealed record GetChatAiRunMetricsByIdQuery(Guid Id) : IRequest<ChatAiRunMetricsEntity?>;

public sealed class GetChatAiRunMetricsByIdQueryHandler
    : IRequestHandler<GetChatAiRunMetricsByIdQuery, ChatAiRunMetricsEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatAiRunMetricsByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatAiRunMetricsEntity?> Handle(
        GetChatAiRunMetricsByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatAiRunMetricsRepository.GetByIdAsync(request.Id, cancellationToken);
}

public sealed record GetChatAiRunMetricsByChatAiRunIdQuery(Guid ChatAiRunId)
    : IRequest<ChatAiRunMetricsEntity?>;

public sealed class GetChatAiRunMetricsByChatAiRunIdQueryHandler
    : IRequestHandler<GetChatAiRunMetricsByChatAiRunIdQuery, ChatAiRunMetricsEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatAiRunMetricsByChatAiRunIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatAiRunMetricsEntity?> Handle(
        GetChatAiRunMetricsByChatAiRunIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatAiRunMetricsRepository.GetByChatAiRunIdAsync(
            request.ChatAiRunId,
            cancellationToken);
}
