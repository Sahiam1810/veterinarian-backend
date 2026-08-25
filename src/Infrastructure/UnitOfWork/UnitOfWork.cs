using Application.Common.Abstractions;
using HelpDesk.Application.Roles.Abstraction;
using Infrastructure.Persistence;

namespace HelpDesk.Infrastructure.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly VeterinaryDbContext _context;

    public UnitOfWork(VeterinaryDbContext context, IRolesRepository rolesRepository)
    {
        _context = context;
        RolesRepository = rolesRepository;
    }

    public IRolesRepository RolesRepository { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
