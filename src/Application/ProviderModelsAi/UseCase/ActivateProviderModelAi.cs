using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Application.ProviderModelsAi.UseCase;

public sealed record ActivateProviderModelAiCommand(Guid Id) : IRequest<ProviderEntity>;

public sealed class ActivateProviderModelAiCommandHandler
    : IRequestHandler<ActivateProviderModelAiCommand, ProviderEntity>
{
    private readonly IUnitOfWork _uow;

    public ActivateProviderModelAiCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ProviderEntity> Handle(
        ActivateProviderModelAiCommand request,
        CancellationToken cancellationToken)
    {
        var provider = await _uow.ProviderModelsAiRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el proveedor de IA '{request.Id}'.");

        provider.Activate();

        await _uow.ProviderModelsAiRepository.UpdateAsync(provider, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return provider;
    }
}
