using Application.Common.Abstractions;
using MediatR;
using RoleEntity = HelpDesk.Domain.Roles.Entities.Roles;

namespace HelpDesk.Application.Roles.UseCase;

public sealed class GetAllRolesQueryHandler
    : IRequestHandler<
        GetAllRolesQuery,
        IReadOnlyCollection<RoleEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllRolesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<RoleEntity>> Handle(
        GetAllRolesQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.RolesRepository.GetAllAsync(
            cancellationToken);
    }
}