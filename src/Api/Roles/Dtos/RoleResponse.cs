namespace HelpDesk.Api.Roles.Dtos;

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt);
