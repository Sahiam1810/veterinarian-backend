using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record UpdateAgentHumanCommand(Guid Id) : IRequest<AgentHumanEntity>;

public sealed class UpdateAgentHumanCommandHandler
    : IRequestHandler<UpdateAgentHumanCommand, AgentHumanEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateAgentHumanCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AgentHumanEntity> Handle(
        UpdateAgentHumanCommand request,
        CancellationToken cancellationToken)
    {
        var agent = await _uow.AgentHumansRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el agente humano '{request.Id}'.");

        // El identificador de usuario no se modifica.
        return agent;
    }
}
