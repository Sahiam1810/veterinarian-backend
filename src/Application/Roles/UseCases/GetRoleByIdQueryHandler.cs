using Application.Common.Abstractions;
using MediatR;
using RoleEntity = HelpDesk.Domain.Roles.Entities.Roles;

namespace HelpDesk.Application.Roles.UseCase;

public sealed class GetRoleByIdQueryHandler
    : IRequestHandler<GetRoleByIdQuery, RoleEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetRoleByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<RoleEntity?> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.RolesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}