using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record CreateAiModelCommand(
    Guid ProviderModelAiId,
    string NameModel,
    string ModelKey,
    decimal InputTokenPrice,
    decimal OutputTokenPrice,
    int MaxTokens,
    int ContextWindow) : IRequest<AiModelEntity>;

public sealed class CreateAiModelCommandHandler
    : IRequestHandler<CreateAiModelCommand, AiModelEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateAiModelCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AiModelEntity> Handle(
        CreateAiModelCommand request,
        CancellationToken cancellationToken)
    {
        var providerExists = await _uow.ProviderModelsAiRepository.ExistsAsync(
            request.ProviderModelAiId,
            cancellationToken);

        if (!providerExists)
        {
            throw new NotFoundException(
                $"No se encontró el proveedor de IA '{request.ProviderModelAiId}'.");
        }

        var model = AiModelEntity.Create(
            request.ProviderModelAiId,
            request.NameModel,
            request.ModelKey,
            request.InputTokenPrice,
            request.OutputTokenPrice,
            request.MaxTokens,
            request.ContextWindow);

        await _uow.AiModelsRepository.AddAsync(model, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return model;
    }
}
