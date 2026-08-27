namespace Api.AgentHumans.Dtos;

public sealed record CreateAgentHumanDto(Guid UserId);

public sealed record AgentHumanResponseDto(
    Guid Id,
    Guid UserId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
