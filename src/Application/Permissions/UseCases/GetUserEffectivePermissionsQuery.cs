using MediatR;

namespace Application.Permissions.UseCases;

public sealed record GetUserEffectivePermissionsQuery(Guid RoleId, Guid UserId)
    : IRequest<IReadOnlyDictionary<string, EffectivePermission>>;
