using Application.Common.Abstractions;
using MediatR;

namespace Application.Permissions.UseCases;

public sealed class GetUserEffectivePermissionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserEffectivePermissionsQuery, IReadOnlyDictionary<string, EffectivePermission>>
{
    public async Task<IReadOnlyDictionary<string, EffectivePermission>> Handle(
        GetUserEffectivePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var modules = await unitOfWork.ModulesRepository.GetAllAsync(cancellationToken);
        var rolePermissions = await unitOfWork.RolePermissionsRepository.GetByRoleIdAsync(
            request.RoleId,
            cancellationToken);
        var userPermissions = await unitOfWork.UserPermissionsRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken);

        var roleByModule = rolePermissions.ToDictionary(permission => permission.ModuleId);
        var userByModule = userPermissions.ToDictionary(permission => permission.ModuleId);

        return modules.ToDictionary(
            module => module.Name.Value,
            module =>
            {
                roleByModule.TryGetValue(module.Id, out var rolePermission);
                userByModule.TryGetValue(module.Id, out var userPermission);

                return new EffectivePermission(
                    (rolePermission?.CanView ?? false) || (userPermission?.CanView ?? false),
                    (rolePermission?.CanCreate ?? false) || (userPermission?.CanCreate ?? false),
                    (rolePermission?.CanEdit ?? false) || (userPermission?.CanEdit ?? false),
                    (rolePermission?.CanDelete ?? false) || (userPermission?.CanDelete ?? false));
            });
    }
}
