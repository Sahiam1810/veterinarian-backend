namespace HelpDesk.Api.Roles.Dtos;

public sealed record CreateRoleRequest(
    string Name,
    string? Description);