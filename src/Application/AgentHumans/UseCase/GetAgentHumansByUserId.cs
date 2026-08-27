using Application.Common.Abstractions;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record GetAgentHumansByUserIdQuery(Guid UserId)
    : IRequest<IReadOnlyCollection<AgentHumanEntity>>;

public sealed class GetAgentHumansByUserIdQueryHandler
    : IRequestHandler<GetAgentHumansByUserIdQuery, IReadOnlyCollection<AgentHumanEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAgentHumansByUserIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<AgentHumanEntity>> Handle(
        GetAgentHumansByUserIdQuery request,
        CancellationToken cancellationToken)
        => _uow.AgentHumansRepository.GetByUserIdAsync(request.UserId, cancellationToken);
}
