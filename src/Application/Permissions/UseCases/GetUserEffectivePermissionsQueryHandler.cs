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

                // Una excepción por usuario reemplaza por completo el permiso del rol
                // para ese módulo (incluida la revocación de algo que el rol sí otorga),
                // en vez de sumarse con OR. Sin excepción, se hereda el permiso del rol.
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
            });
    }
}
