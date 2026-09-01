using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using RoleEntity = Domain.Roles.Entities.Roles;

namespace Application.Roles.UseCase;

public sealed class GetRoleByIdQueryHandler
    : IRequestHandler<GetRoleByIdQuery, RoleEntity>
{
    private readonly IUnitOfWork _uow;

    public GetRoleByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<RoleEntity> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.RolesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Rol no encontrado.");
    }
}