namespace HelpDesk.Api.Roles.Dtos;

public sealed record UpdateRoleRequest(
    string Name,
    string? Description);
