using MediatR;

namespace Application.Permissions.UseCases;

public sealed record GetUserPermissionClaimsQuery(Guid RoleId)
    : IRequest<IReadOnlyCollection<string>>;
