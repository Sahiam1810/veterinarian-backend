using MediatR;
using RoleEntity = HelpDesk.Domain.Roles.Entities.Roles;

namespace HelpDesk.Application.Roles.UseCase;

public sealed record GetAllRolesQuery
    : IRequest<IReadOnlyCollection<RoleEntity>>;