using Application.Common.Abstractions;
using Application.Common.Exceptions;
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
    private readonly IUnitOfWork _uow;

    public UpdateProviderModelAiCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ProviderEntity> Handle(
        UpdateProviderModelAiCommand request,
        CancellationToken cancellationToken)
    {
        var provider = await _uow.ProviderModelsAiRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el proveedor de IA '{request.Id}'.");

        provider.Update(request.NameProviderAi, request.BusinessName, request.Website);

        await _uow.ProviderModelsAiRepository.UpdateAsync(provider, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return provider;
    }
}
