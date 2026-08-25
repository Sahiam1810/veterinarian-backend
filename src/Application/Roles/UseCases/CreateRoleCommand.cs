using MediatR;

namespace HelpDesk.Application.Roles.UseCase;

public sealed record CreateRoleCommand(
    string Name,
    string? Description) : IRequest<Guid>;