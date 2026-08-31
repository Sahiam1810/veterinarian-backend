using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatAiRunEntity = Domain.ChatAiRuns.Entities.ChatAiRun;

namespace Application.ChatAiRuns.UseCase;

public sealed record UpdateChatAiRunStatusCommand(
    Guid Id,
    Guid AiRunStatusId) : IRequest<ChatAiRunEntity>;

public sealed class UpdateChatAiRunStatusCommandHandler
    : IRequestHandler<UpdateChatAiRunStatusCommand, ChatAiRunEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateChatAiRunStatusCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatAiRunEntity> Handle(
        UpdateChatAiRunStatusCommand request,
        CancellationToken cancellationToken)
    {
        var chatAiRun = await _uow.ChatAiRunsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la ejecución de IA '{request.Id}'.");

        var aiRunStatus = await _uow.AiRunStatusesRepository.GetByIdAsync(
            request.AiRunStatusId,
            cancellationToken);
        if (aiRunStatus is null)
        {
            throw new NotFoundException(
                $"No se encontró el estado de ejecución '{request.AiRunStatusId}'.");
        }

        chatAiRun.UpdateStatus(request.AiRunStatusId);

        await _uow.ChatAiRunsRepository.UpdateAsync(chatAiRun, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return chatAiRun;
    }
}
