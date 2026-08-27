using Application.AgentHumans.Abstraction;
using Application.Common.Exceptions;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record DeactivateAgentHumanCommand(Guid Id) : IRequest<AgentHumanEntity>;

public sealed class DeactivateAgentHumanCommandHandler
    : IRequestHandler<DeactivateAgentHumanCommand, AgentHumanEntity>
{
    private readonly IAgentHumanRepository _repository;

    public DeactivateAgentHumanCommandHandler(IAgentHumanRepository repository)
    {
        _repository = repository;
    }

    public async Task<AgentHumanEntity> Handle(
        DeactivateAgentHumanCommand request,
        CancellationToken cancellationToken)
    {
        var agent = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el agente humano '{request.Id}'.");

        agent.Deactivate();

        await _repository.UpdateAsync(agent, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return agent;
    }
}
