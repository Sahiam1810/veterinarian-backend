using Application.Common.Abstractions;
using MediatR;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Application.ProviderModelsAi.UseCase;

public sealed record GetAllProviderModelAisQuery : IRequest<IReadOnlyCollection<ProviderEntity>>;

public sealed class GetAllProviderModelAisQueryHandler
    : IRequestHandler<GetAllProviderModelAisQuery, IReadOnlyCollection<ProviderEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllProviderModelAisQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ProviderEntity>> Handle(
        GetAllProviderModelAisQuery request,
        CancellationToken cancellationToken)
        => _uow.ProviderModelsAiRepository.GetAllAsync(cancellationToken);
}
