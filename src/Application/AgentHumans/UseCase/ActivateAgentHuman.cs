using Application.AgentHumans.Abstraction;
using Application.Common.Exceptions;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record ActivateAgentHumanCommand(Guid Id) : IRequest<AgentHumanEntity>;

public sealed class ActivateAgentHumanCommandHandler
    : IRequestHandler<ActivateAgentHumanCommand, AgentHumanEntity>
{
    private readonly IAgentHumanRepository _repository;

    public ActivateAgentHumanCommandHandler(IAgentHumanRepository repository)
    {
        _repository = repository;
    }

    public async Task<AgentHumanEntity> Handle(
        ActivateAgentHumanCommand request,
        CancellationToken cancellationToken)
    {
        var agent = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el agente humano '{request.Id}'.");

        agent.Activate();

        await _repository.UpdateAsync(agent, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return agent;
    }
}
