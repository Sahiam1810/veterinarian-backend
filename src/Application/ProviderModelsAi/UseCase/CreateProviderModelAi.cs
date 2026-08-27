using Application.Common.Abstractions;
using MediatR;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Application.ProviderModelsAi.UseCase;

public sealed record CreateProviderModelAiCommand(
    string NameProviderAi,
    string? BusinessName,
    string? Website) : IRequest<ProviderEntity>;

public sealed class CreateProviderModelAiCommandHandler
    : IRequestHandler<CreateProviderModelAiCommand, ProviderEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateProviderModelAiCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ProviderEntity> Handle(
        CreateProviderModelAiCommand request,
        CancellationToken cancellationToken)
    {
        var provider = ProviderEntity.Create(
            request.NameProviderAi,
            request.BusinessName,
            request.Website);

        await _uow.ProviderModelsAiRepository.AddAsync(provider, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return provider;
    }
}
