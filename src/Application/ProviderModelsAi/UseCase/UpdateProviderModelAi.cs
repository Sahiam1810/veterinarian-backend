using Application.Common.Exceptions;
using Application.ProviderModelsAi.Abstraction;
using MediatR;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Application.ProviderModelsAi.UseCase;

public sealed record UpdateProviderModelAiCommand(
    Guid Id,
    string NameProviderAi,
    string? BusinessName,
    string? Website) : IRequest<ProviderEntity>;

public sealed class UpdateProviderModelAiCommandHandler
    : IRequestHandler<UpdateProviderModelAiCommand, ProviderEntity>
{
    private readonly IProviderModelAiRepository _repository;

    public UpdateProviderModelAiCommandHandler(IProviderModelAiRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProviderEntity> Handle(
        UpdateProviderModelAiCommand request,
        CancellationToken cancellationToken)
    {
        var provider = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el proveedor de IA '{request.Id}'.");

        provider.Update(request.NameProviderAi, request.BusinessName, request.Website);

        await _repository.UpdateAsync(provider, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return provider;
    }
}
