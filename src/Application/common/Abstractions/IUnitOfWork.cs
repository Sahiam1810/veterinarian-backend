using HelpDesk.Application.Roles.Abstraction;

namespace Application.Common.Abstractions;

public interface IUnitOfWork
{
    IRolesRepository RolesRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
