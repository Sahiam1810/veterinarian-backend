using Application.AiModels.Abstraction;
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
    private readonly IAiModelRepository _repository;

    public UpdateAiModelCommandHandler(IAiModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<AiModelEntity> Handle(
        UpdateAiModelCommand request,
        CancellationToken cancellationToken)
    {
        var model = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el modelo de IA '{request.Id}'.");

        model.Update(
            request.NameModel,
            request.ModelKey,
            request.InputTokenPrice,
            request.OutputTokenPrice,
            request.MaxTokens,
            request.ContextWindow);

        await _repository.UpdateAsync(model, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return model;
    }
}
