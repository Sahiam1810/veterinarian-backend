using Application.AgentHumans.Abstraction;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record GetAgentHumansByUserIdQuery(Guid UserId)
    : IRequest<IReadOnlyCollection<AgentHumanEntity>>;

public sealed class GetAgentHumansByUserIdQueryHandler
    : IRequestHandler<GetAgentHumansByUserIdQuery, IReadOnlyCollection<AgentHumanEntity>>
{
    private readonly IAgentHumanRepository _repository;

    public GetAgentHumansByUserIdQueryHandler(IAgentHumanRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<AgentHumanEntity>> Handle(
        GetAgentHumansByUserIdQuery request,
        CancellationToken cancellationToken)
        => _repository.GetByUserIdAsync(request.UserId, cancellationToken);
}
