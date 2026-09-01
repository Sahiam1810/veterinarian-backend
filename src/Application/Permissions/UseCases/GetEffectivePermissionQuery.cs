using MediatR;

namespace Application.Permissions.UseCases;

// Combina el permiso del rol con el permiso puntual del usuario (aditivo:
// el puntual solo puede sumar, nunca quitar lo que ya da el rol).
public sealed record GetEffectivePermissionQuery(Guid RoleId, Guid UserId, string ModuleName)
    : IRequest<EffectivePermission>;

public sealed record EffectivePermission(bool CanView, bool CanCreate, bool CanEdit, bool CanDelete)
{
    public static readonly EffectivePermission None = new(false, false, false, false);
}
