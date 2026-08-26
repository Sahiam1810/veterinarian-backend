using Application.AiModels.Abstraction;
using Application.Common.Exceptions;
using Application.ProviderModelsAi.Abstraction;
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
    private readonly IAiModelRepository _modelRepository;
    private readonly IProviderModelAiRepository _providerRepository;

    public CreateAiModelCommandHandler(
        IAiModelRepository modelRepository,
        IProviderModelAiRepository providerRepository)
    {
        _modelRepository = modelRepository;
        _providerRepository = providerRepository;
    }

    public async Task<AiModelEntity> Handle(
        CreateAiModelCommand request,
        CancellationToken cancellationToken)
    {
        var providerExists = await _providerRepository.ExistsAsync(
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

        await _modelRepository.AddAsync(model, cancellationToken);
        await _modelRepository.SaveChangesAsync(cancellationToken);

        return model;
    }
}
