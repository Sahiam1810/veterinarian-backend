using Application.Common.Exceptions;
using Application.ProviderModelsAi.Abstraction;
using MediatR;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Application.ProviderModelsAi.UseCase;

public sealed record ActivateProviderModelAiCommand(Guid Id) : IRequest<ProviderEntity>;

public sealed class ActivateProviderModelAiCommandHandler
    : IRequestHandler<ActivateProviderModelAiCommand, ProviderEntity>
{
    private readonly IProviderModelAiRepository _repository;

    public ActivateProviderModelAiCommandHandler(IProviderModelAiRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProviderEntity> Handle(
        ActivateProviderModelAiCommand request,
        CancellationToken cancellationToken)
    {
        var provider = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el proveedor de IA '{request.Id}'.");

        provider.Activate();

        await _repository.UpdateAsync(provider, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return provider;
    }
}
