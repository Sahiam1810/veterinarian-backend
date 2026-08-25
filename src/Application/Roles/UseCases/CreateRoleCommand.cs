using MediatR;

namespace Application.Roles.UseCase;

public sealed record CreateRoleCommand(
    string Name,
    string? Description) : IRequest<Guid>;