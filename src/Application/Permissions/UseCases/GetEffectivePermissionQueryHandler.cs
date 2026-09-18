using Application.Common.Abstractions;
using MediatR;

namespace Application.Permissions.UseCases;

public sealed class GetEffectivePermissionQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetEffectivePermissionQuery, EffectivePermission>
{
    public async Task<EffectivePermission> Handle(
        GetEffectivePermissionQuery request,
        CancellationToken cancellationToken)
    {
        var rolePermission = await unitOfWork.RolePermissionsRepository.GetByRoleAndModuleNameAsync(
            request.RoleId,
            request.ModuleName,
            cancellationToken);

        var userPermission = await unitOfWork.UserPermissionsRepository.GetByUserAndModuleNameAsync(
            request.UserId,
            request.ModuleName,
            cancellationToken);

        if (rolePermission is null && userPermission is null)
        {
            return EffectivePermission.None;
        }

        // Una excepción por usuario reemplaza por completo el permiso del rol para
        // ese módulo (incluida la revocación de algo que el rol sí otorga), en vez
        // de sumarse con OR. Sin excepción, se hereda el permiso del rol.
        if (userPermission is not null)
        {
            return new EffectivePermission(
                userPermission.CanView,
                userPermission.CanCreate,
                userPermission.CanEdit,
                userPermission.CanDelete);
        }

        return new EffectivePermission(
            rolePermission?.CanView ?? false,
            rolePermission?.CanCreate ?? false,
            rolePermission?.CanEdit ?? false,
            rolePermission?.CanDelete ?? false);
    }
}
