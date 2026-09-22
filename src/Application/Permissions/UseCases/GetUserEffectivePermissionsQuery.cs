using MediatR;

namespace Application.Permissions.UseCases;

public sealed record GetUserEffectivePermissionsQuery(Guid RoleId)
    : IRequest<IReadOnlyDictionary<string, EffectivePermission>>;

public sealed record EffectivePermission(bool CanView, bool CanCreate, bool CanEdit, bool CanDelete)
{
    public static readonly EffectivePermission None = new(false, false, false, false);
}
