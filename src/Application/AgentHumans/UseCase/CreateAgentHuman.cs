using Application.AgentHumans.Abstraction;
using Application.Common.Exceptions;
using Application.Users.Abstraction;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record CreateAgentHumanCommand(Guid UserId) : IRequest<AgentHumanEntity>;

public sealed class CreateAgentHumanCommandHandler
    : IRequestHandler<CreateAgentHumanCommand, AgentHumanEntity>
{
    private readonly IAgentHumanRepository _agentRepository;
    private readonly IUsersRepository _usersRepository;

    public CreateAgentHumanCommandHandler(
        IAgentHumanRepository agentRepository,
        IUsersRepository usersRepository)
    {
        _agentRepository = agentRepository;
        _usersRepository = usersRepository;
    }

    public async Task<AgentHumanEntity> Handle(
        CreateAgentHumanCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _usersRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException($"No se encontró el usuario '{request.UserId}'.");
        }

        var agent = AgentHumanEntity.Create(request.UserId);

        await _agentRepository.AddAsync(agent, cancellationToken);
        await _agentRepository.SaveChangesAsync(cancellationToken);

        return agent;
    }
}
