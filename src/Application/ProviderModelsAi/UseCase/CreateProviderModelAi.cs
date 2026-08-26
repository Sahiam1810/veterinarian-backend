using Application.ProviderModelsAi.Abstraction;
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
    private readonly IProviderModelAiRepository _repository;

    public CreateProviderModelAiCommandHandler(IProviderModelAiRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProviderEntity> Handle(
        CreateProviderModelAiCommand request,
        CancellationToken cancellationToken)
    {
        var provider = ProviderEntity.Create(
            request.NameProviderAi,
            request.BusinessName,
            request.Website);

        await _repository.AddAsync(provider, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return provider;
    }
}
