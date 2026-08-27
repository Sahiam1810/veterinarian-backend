using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Application.AgentHumans.UseCase;

public sealed record CreateAgentHumanCommand(Guid UserId) : IRequest<AgentHumanEntity>;

public sealed class CreateAgentHumanCommandHandler
    : IRequestHandler<CreateAgentHumanCommand, AgentHumanEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateAgentHumanCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AgentHumanEntity> Handle(
        CreateAgentHumanCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException($"No se encontró el usuario '{request.UserId}'.");
        }

        var agent = AgentHumanEntity.Create(request.UserId);

        await _uow.AgentHumansRepository.AddAsync(agent, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return agent;
    }
}
