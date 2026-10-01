using Application.Common.Abstractions;
using Domain.Roles;
using MediatR;
using RoleEntity = Domain.Roles.Entities.Roles;

namespace Application.Roles.UseCase;

public sealed class GetAllRolesQueryHandler
    : IRequestHandler<
        GetAllRolesQuery,
        IReadOnlyCollection<RoleEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllRolesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<RoleEntity>> Handle(
        GetAllRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _uow.RolesRepository.GetAllAsync(
            cancellationToken);

        // Cliente es el rol técnico del bot/portal; no se administra desde Usuarios/Roles.
        return roles
            .Where(role => role.Id != SystemRoles.ClientRoleId)
            .ToArray();
    }
}