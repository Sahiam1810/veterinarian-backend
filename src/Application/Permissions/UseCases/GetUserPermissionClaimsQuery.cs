using MediatR;

namespace Application.Permissions.UseCases;

public sealed record GetUserPermissionClaimsQuery(Guid RoleId, Guid UserId)
    : IRequest<IReadOnlyCollection<string>>;
