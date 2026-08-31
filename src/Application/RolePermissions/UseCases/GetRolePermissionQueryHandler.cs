using Application.Common.Abstractions;
using MediatR;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;

namespace Application.RolePermissions.UseCases;

public sealed class GetRolePermissionQueryHandler
    : IRequestHandler<GetRolePermissionQuery, RolePermissionEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetRolePermissionQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<RolePermissionEntity?> Handle(
        GetRolePermissionQuery request,
        CancellationToken cancellationToken)
        => _uow.RolePermissionsRepository.GetByRoleAndModuleNameAsync(
            request.RoleId,
            request.ModuleName,
            cancellationToken);
}
