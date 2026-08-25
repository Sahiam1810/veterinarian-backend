using Application.Common.Abstractions;
using MediatR;

namespace Application.Roles.UseCase;

public sealed class DeleteRoleCommandHandler
    : IRequestHandler<DeleteRoleCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteRoleCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _uow.RolesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (role is null)
        {
            return false;
        }

        await _uow.RolesRepository.DeleteAsync(
            role,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
