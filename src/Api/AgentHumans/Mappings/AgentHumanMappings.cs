using Api.AgentHumans.Dtos;
using Application.AgentHumans.UseCase;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;

namespace Api.AgentHumans.Mappings;

public static class AgentHumanMappings
{
    public static CreateAgentHumanCommand ToCommand(this CreateAgentHumanDto dto)
        => new(dto.UserId);

    public static AgentHumanResponseDto ToResponse(this AgentHumanEntity agent)
        => new(
            agent.Id,
            agent.UserId,
            agent.IsActive,
            agent.CreatedAt,
            agent.UpdatedAt);

    public static IReadOnlyCollection<AgentHumanResponseDto> ToResponse(
        this IReadOnlyCollection<AgentHumanEntity> agents)
        => agents.Select(agent => agent.ToResponse()).ToArray();
}
