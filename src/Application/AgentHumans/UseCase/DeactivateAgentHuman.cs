using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record DeactivateAgentHumanCommand(Guid Id) : IRequest<AgentHumanEntity>;

public sealed class DeactivateAgentHumanCommandHandler
    : IRequestHandler<DeactivateAgentHumanCommand, AgentHumanEntity>
{
    private readonly IUnitOfWork _uow;

    public DeactivateAgentHumanCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AgentHumanEntity> Handle(
        DeactivateAgentHumanCommand request,
        CancellationToken cancellationToken)
    {
        var agent = await _uow.AgentHumansRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el agente humano '{request.Id}'.");

        agent.Deactivate();

        await _uow.AgentHumansRepository.UpdateAsync(agent, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return agent;
    }
}
