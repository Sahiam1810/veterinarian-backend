using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatAiRunErrorEntity = Domain.ChatAiRunErrors.Entities.ChatAiRunError;

namespace Application.ChatAiRunErrors.UseCase;

public sealed record CreateChatAiRunErrorCommand(
    Guid ChatAiRunId,
    string ErrorMessage,
    string? ErrorCode,
    string? ProviderErrorId) : IRequest<ChatAiRunErrorEntity>;

public sealed class CreateChatAiRunErrorCommandHandler
    : IRequestHandler<CreateChatAiRunErrorCommand, ChatAiRunErrorEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatAiRunErrorCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatAiRunErrorEntity> Handle(
        CreateChatAiRunErrorCommand request,
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

        var error = ChatAiRunErrorEntity.Create(
            request.ChatAiRunId,
            request.ErrorMessage,
            request.ErrorCode,
            request.ProviderErrorId);

        await _uow.ChatAiRunErrorsRepository.AddAsync(error, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return error;
    }
}
