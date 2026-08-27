using Application.Common.Abstractions;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record GetAllAgentHumansQuery : IRequest<IReadOnlyCollection<AgentHumanEntity>>;

public sealed class GetAllAgentHumansQueryHandler
    : IRequestHandler<GetAllAgentHumansQuery, IReadOnlyCollection<AgentHumanEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllAgentHumansQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<AgentHumanEntity>> Handle(
        GetAllAgentHumansQuery request,
        CancellationToken cancellationToken)
        => _uow.AgentHumansRepository.GetAllAsync(cancellationToken);
}
