using Application.Permissions.Claims;
using MediatR;

namespace Application.Permissions.UseCases;

public sealed class GetUserPermissionClaimsQueryHandler(ISender sender)
    : IRequestHandler<GetUserPermissionClaimsQuery, IReadOnlyCollection<string>>
{
    public async Task<IReadOnlyCollection<string>> Handle(
        GetUserPermissionClaimsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await sender.Send(
            new GetUserEffectivePermissionsQuery(request.RoleId, request.UserId),
            cancellationToken);
        var claims = new List<string>();

        foreach (var (moduleName, permission) in permissions)
        {
            AddIfGranted(claims, moduleName, "View", permission.CanView);
            AddIfGranted(claims, moduleName, "Create", permission.CanCreate);
            AddIfGranted(claims, moduleName, "Edit", permission.CanEdit);
            AddIfGranted(claims, moduleName, "Delete", permission.CanDelete);
        }

        return claims
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddIfGranted(
        ICollection<string> claims,
        string moduleName,
        string action,
        bool granted)
    {
        if (granted)
        {
            claims.Add(PermissionClaimValue.Create(moduleName, action));
        }
    }
}
