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

        return new EffectivePermission(
            (rolePermission?.CanView ?? false) || (userPermission?.CanView ?? false),
            (rolePermission?.CanCreate ?? false) || (userPermission?.CanCreate ?? false),
            (rolePermission?.CanEdit ?? false) || (userPermission?.CanEdit ?? false),
            (rolePermission?.CanDelete ?? false) || (userPermission?.CanDelete ?? false));
    }
}
