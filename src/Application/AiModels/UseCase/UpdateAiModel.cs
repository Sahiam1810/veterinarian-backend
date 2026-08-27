using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record UpdateAiModelCommand(
    Guid Id,
    string NameModel,
    string ModelKey,
    decimal InputTokenPrice,
    decimal OutputTokenPrice,
    int MaxTokens,
    int ContextWindow) : IRequest<AiModelEntity>;

public sealed class UpdateAiModelCommandHandler
    : IRequestHandler<UpdateAiModelCommand, AiModelEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateAiModelCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AiModelEntity> Handle(
        UpdateAiModelCommand request,
        CancellationToken cancellationToken)
    {
        var model = await _uow.AiModelsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el modelo de IA '{request.Id}'.");

        model.Update(
            request.NameModel,
            request.ModelKey,
            request.InputTokenPrice,
            request.OutputTokenPrice,
            request.MaxTokens,
            request.ContextWindow);

        await _uow.AiModelsRepository.UpdateAsync(model, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return model;
    }
}
