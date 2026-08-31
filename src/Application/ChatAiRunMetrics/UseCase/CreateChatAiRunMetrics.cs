using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatAiRunMetricsEntity = Domain.ChatAiRunMetrics.Entities.ChatAiRunMetrics;

namespace Application.ChatAiRunMetrics.UseCase;

public sealed record CreateChatAiRunMetricsCommand(
    Guid ChatAiRunId,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal Cost) : IRequest<ChatAiRunMetricsEntity>;

public sealed class CreateChatAiRunMetricsCommandHandler
    : IRequestHandler<CreateChatAiRunMetricsCommand, ChatAiRunMetricsEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatAiRunMetricsCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatAiRunMetricsEntity> Handle(
        CreateChatAiRunMetricsCommand request,
        CancellationToken cancellationToken)
    {
        var chatAiRun = await _uow.ChatAiRunsRepository.GetByIdAsync(
            request.ChatAiRunId,
            cancellationToken);
        if (chatAiRun is null)
        {
            throw new NotFoundException(
                $"No se encontró la ejecución de IA '{request.ChatAiRunId}'.");
        }

        if (await _uow.ChatAiRunMetricsRepository.ExistsByChatAiRunIdAsync(
                request.ChatAiRunId,
                cancellationToken))
        {
            throw new ConflictException(
                $"Ya existen métricas para la ejecución de IA '{request.ChatAiRunId}'.");
        }

        var metrics = ChatAiRunMetricsEntity.Create(
            request.ChatAiRunId,
            request.PromptTokens,
            request.CompletionTokens,
            request.TotalTokens,
            request.Cost);

        await _uow.ChatAiRunMetricsRepository.AddAsync(metrics, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return metrics;
    }
}
