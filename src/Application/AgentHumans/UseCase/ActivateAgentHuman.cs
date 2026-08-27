using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record ActivateAgentHumanCommand(Guid Id) : IRequest<AgentHumanEntity>;

public sealed class ActivateAgentHumanCommandHandler
    : IRequestHandler<ActivateAgentHumanCommand, AgentHumanEntity>
{
    private readonly IUnitOfWork _uow;

    public ActivateAgentHumanCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AgentHumanEntity> Handle(
        ActivateAgentHumanCommand request,
        CancellationToken cancellationToken)
    {
        var agent = await _uow.AgentHumansRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el agente humano '{request.Id}'.");

        agent.Activate();

        await _uow.AgentHumansRepository.UpdateAsync(agent, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return agent;
    }
}
