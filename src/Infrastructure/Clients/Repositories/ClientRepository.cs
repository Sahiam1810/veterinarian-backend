using Application.Clients.Abstraction;
using Domain.Clients.Entities;
using Domain.Clients.ValueObjects; // ðŸ‘ˆ 1. Importante para usar ClientIdentificationNumber
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

    // ðŸ‘ˆ 2. CORREGIDO: Compara contra el Value Object
    public async Task<ClientEntity?> GetByIdentificationNumberAsync(string identificationNumber, CancellationToken cancellationToken)
    {
        var idVo = ClientIdentificationNumber.Create(identificationNumber);

        return await _context.Set<ClientEntity>()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.IdentificationNumber == idVo, cancellationToken);
    }

    // Match exacto por VO normalizado (solo dÃ­gitos). Unicidad BD: UX_CLIENTS_PHONE_NUMBER.
    public async Task<ClientEntity?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var phoneVo = ClientPhoneNumber.Create(phoneNumber);

        return await _context.Set<ClientEntity>()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.PhoneNumber == phoneVo, cancellationToken);
    }

    public async Task<ClientEntity?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Set<ClientEntity>()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<ClientEntity?> GetByLookupAsync(
        string? identificationNumber,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        var query = _context.Set<ClientEntity>()
            .Include(c => c.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(identificationNumber))
        {
            var idVo = ClientIdentificationNumber.Create(identificationNumber);
            query = query.Where(c => c.IdentificationNumber == idVo);
        }

        // TelÃ©fono obligatorio en este filtro: Create (no Optional) tras el validator.
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var phoneVo = ClientPhoneNumber.Create(phoneNumber);
            query = query.Where(c => c.PhoneNumber == phoneVo);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    // ðŸ‘ˆ 3. CORREGIDO: Compara contra el Value Object sin usar '.Value'
    public async Task<bool> ExistsByIdentificationNumberAsync(string identificationNumber, CancellationToken cancellationToken, Guid? excludedId = null)
    {
        var idVo = ClientIdentificationNumber.Create(identificationNumber);

        var query = _context.Set<ClientEntity>()
            .Where(c => c.IdentificationNumber == idVo);

        if (excludedId.HasValue)
        {
            query = query.Where(c => c.Id != excludedId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByPhoneAsync(
        string phoneNumber,
        CancellationToken cancellationToken,
        Guid? excludedId = null)
    {
        var phoneVo = ClientPhoneNumber.Create(phoneNumber);

        var query = _context.Set<ClientEntity>()
            .Where(c => c.PhoneNumber == phoneVo);

        if (excludedId.HasValue)
        {
            query = query.Where(c => c.Id != excludedId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken, Guid? excludedId = null)
    {
        var query = _context.Set<ClientEntity>()
            .Where(c => c.UserId == userId);

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

    public async Task<Guid?> GetIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => await _context.Set<ClientEntity>()
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task DeleteByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var client = await _context.Set<ClientEntity>()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (client is null)
        {
            return;
        }

        _context.Set<ClientEntity>().Remove(client);
    }
}
