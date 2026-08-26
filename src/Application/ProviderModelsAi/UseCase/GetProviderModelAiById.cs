using Application.ProviderModelsAi.Abstraction;
using MediatR;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Application.ProviderModelsAi.UseCase;

public sealed record GetProviderModelAiByIdQuery(Guid Id) : IRequest<ProviderEntity?>;

public sealed class GetProviderModelAiByIdQueryHandler
    : IRequestHandler<GetProviderModelAiByIdQuery, ProviderEntity?>
{
    private readonly IProviderModelAiRepository _repository;

    public GetProviderModelAiByIdQueryHandler(IProviderModelAiRepository repository)
    {
        _repository = repository;
    }

    public Task<ProviderEntity?> Handle(
        GetProviderModelAiByIdQuery request,
        CancellationToken cancellationToken)
        => _repository.GetByIdAsync(request.Id, cancellationToken);
}
