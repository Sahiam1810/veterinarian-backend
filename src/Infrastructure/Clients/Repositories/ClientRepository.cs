using Application.Clients.Abstraction;
using Domain.Clients.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Clients.Repositories;

public sealed class ClientRepository : IClientRepository
{
    private readonly VeterinaryDbContext _context;

    public ClientRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ClientEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<ClientEntity>()
            .Include(c => c.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Set<ClientEntity>()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<ClientEntity?> GetByIdentificationNumberAsync(string identificationNumber, CancellationToken cancellationToken)
    {
        return await _context.Set<ClientEntity>()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.IdentificationNumber.Value == identificationNumber, cancellationToken);
    }

    public async Task<ClientEntity?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Set<ClientEntity>()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsByIdentificationNumberAsync(string identificationNumber, CancellationToken cancellationToken, Guid? excludedId = null)
    {
        var query = _context.Set<ClientEntity>().Where(c => c.IdentificationNumber.Value == identificationNumber);
        if (excludedId.HasValue)
        {
            query = query.Where(c => c.Id != excludedId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(ClientEntity client, CancellationToken cancellationToken)
    {
        await _context.Set<ClientEntity>().AddAsync(client, cancellationToken);
    }

    public Task UpdateAsync(ClientEntity client, CancellationToken cancellationToken)
    {
        _context.Set<ClientEntity>().Update(client);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ClientEntity client, CancellationToken cancellationToken)
    {
        _context.Set<ClientEntity>().Remove(client);
        return Task.CompletedTask;
    }
}
