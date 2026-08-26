using Application.ProviderModelsAi.Abstraction;
using MediatR;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Application.ProviderModelsAi.UseCase;

public sealed record GetAllProviderModelAisQuery : IRequest<IReadOnlyCollection<ProviderEntity>>;

public sealed class GetAllProviderModelAisQueryHandler
    : IRequestHandler<GetAllProviderModelAisQuery, IReadOnlyCollection<ProviderEntity>>
{
    private readonly IProviderModelAiRepository _repository;

    public GetAllProviderModelAisQueryHandler(IProviderModelAiRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<ProviderEntity>> Handle(
        GetAllProviderModelAisQuery request,
        CancellationToken cancellationToken)
        => _repository.GetAllAsync(cancellationToken);
}
