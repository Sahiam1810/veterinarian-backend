using Application.AgentHumans.Abstraction;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record GetAgentHumanByIdQuery(Guid Id) : IRequest<AgentHumanEntity?>;

public sealed class GetAgentHumanByIdQueryHandler
    : IRequestHandler<GetAgentHumanByIdQuery, AgentHumanEntity?>
{
    private readonly IAgentHumanRepository _repository;

    public GetAgentHumanByIdQueryHandler(IAgentHumanRepository repository)
    {
        _repository = repository;
    }

    public Task<AgentHumanEntity?> Handle(
        GetAgentHumanByIdQuery request,
        CancellationToken cancellationToken)
        => _repository.GetByIdAsync(request.Id, cancellationToken);
}
