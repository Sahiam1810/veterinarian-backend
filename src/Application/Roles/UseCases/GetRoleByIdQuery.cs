using MediatR;
using RoleEntity = Domain.Roles.Entities.Roles;

namespace Application.Roles.UseCase;

public sealed record GetRoleByIdQuery(Guid Id)
    : IRequest<RoleEntity?>;