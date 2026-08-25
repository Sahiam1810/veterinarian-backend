using MediatR;
using RoleEntity = HelpDesk.Domain.Roles.Entities.Roles;

namespace HelpDesk.Application.Roles.UseCase;

public sealed record GetRoleByIdQuery(Guid Id)
    : IRequest<RoleEntity?>;