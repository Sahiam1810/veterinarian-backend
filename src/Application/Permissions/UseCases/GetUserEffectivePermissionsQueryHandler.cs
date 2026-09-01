using Application.Common.Abstractions;
using Application.Modules.UseCases;
using MediatR;

namespace Application.Permissions.UseCases;

public sealed class GetUserEffectivePermissionsQueryHandler(IUnitOfWork unitOfWork, ISender sender)
    : IRequestHandler<GetUserEffectivePermissionsQuery, IReadOnlyDictionary<string, EffectivePermission>>
{
    public async Task<IReadOnlyDictionary<string, EffectivePermission>> Handle(
        GetUserEffectivePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var modules = await unitOfWork.ModulesRepository.GetAllAsync(cancellationToken);
        var permissions = new Dictionary<string, EffectivePermission>();

        foreach (var module in modules)
        {
            var permission = await sender.Send(
                new GetEffectivePermissionQuery(request.RoleId, request.UserId, module.Name.Value),
                cancellationToken);
            permissions[module.Name.Value] = permission;
        }

        return permissions;
    }
}
