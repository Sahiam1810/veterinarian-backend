using MediatR;

namespace HelpDesk.Application.Roles.UseCase;

public sealed record UpdateRoleCommand(
    Guid Id,
    string Name,
    string? Description) : IRequest<bool>;
