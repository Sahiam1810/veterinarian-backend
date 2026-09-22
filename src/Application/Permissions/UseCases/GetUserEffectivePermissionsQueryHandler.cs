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

        var roleByModule = rolePermissions.ToDictionary(permission => permission.ModuleId);

        return modules.ToDictionary(
            module => module.Name.Value,
            module =>
            {
                roleByModule.TryGetValue(module.Id, out var rolePermission);

                return new EffectivePermission(
                    rolePermission?.CanView ?? false,
                    rolePermission?.CanCreate ?? false,
                    rolePermission?.CanEdit ?? false,
                    rolePermission?.CanDelete ?? false);
            });
    }
}
