using Application.AgentHumans.Abstraction;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record GetAllAgentHumansQuery : IRequest<IReadOnlyCollection<AgentHumanEntity>>;

public sealed class GetAllAgentHumansQueryHandler
    : IRequestHandler<GetAllAgentHumansQuery, IReadOnlyCollection<AgentHumanEntity>>
{
    private readonly IAgentHumanRepository _repository;

    public GetAllAgentHumansQueryHandler(IAgentHumanRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<AgentHumanEntity>> Handle(
        GetAllAgentHumansQuery request,
        CancellationToken cancellationToken)
        => _repository.GetAllAsync(cancellationToken);
}
