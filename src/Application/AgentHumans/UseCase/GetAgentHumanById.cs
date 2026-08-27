using Application.Common.Abstractions;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record GetAgentHumanByIdQuery(Guid Id) : IRequest<AgentHumanEntity?>;

public sealed class GetAgentHumanByIdQueryHandler
    : IRequestHandler<GetAgentHumanByIdQuery, AgentHumanEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetAgentHumanByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<AgentHumanEntity?> Handle(
        GetAgentHumanByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.AgentHumansRepository.GetByIdAsync(request.Id, cancellationToken);
}
