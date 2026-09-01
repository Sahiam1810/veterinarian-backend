using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Roles.UseCase;

public sealed class DeleteRoleCommandHandler
    : IRequestHandler<DeleteRoleCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteRoleCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _uow.RolesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Rol no encontrado.");

        await _uow.RolesRepository.DeleteAsync(
            role,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
