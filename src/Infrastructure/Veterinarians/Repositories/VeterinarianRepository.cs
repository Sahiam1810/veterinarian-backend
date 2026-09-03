using Application.Veterinarians.Abstraction;
using Domain.Veterinarians.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Veterinarians.Repositories;

public sealed class VeterinarianRepository : IVeterinarianRepository
{
    private readonly VeterinaryDbContext _context;

    public VeterinarianRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Veterinarian>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<Veterinarian>()
            .Include(x => x.User)
            .Include(x => x.Specialty)
            .AsNoTracking()
            .OrderBy(x => x.LicenseNumber)
            .ToListAsync(cancellationToken);

    public Task<Veterinarian?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<Veterinarian>()
            .Include(x => x.User)
            .Include(x => x.Specialty)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Veterinarian?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _context.Set<Veterinarian>()
            .Include(x => x.User)
            .Include(x => x.Specialty)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task<bool> ExistsByLicenseNumberAsync(
        string licenseNumber,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
        => _context.Set<Veterinarian>()
            .AnyAsync(
                x => x.LicenseNumber == licenseNumber
                    && (!excludedId.HasValue || x.Id != excludedId.Value),
                cancellationToken);

    public Task<bool> ExistsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
        => _context.Set<Veterinarian>()
            .AnyAsync(
                x => x.UserId == userId
                    && (!excludedId.HasValue || x.Id != excludedId.Value),
                cancellationToken);

    public async Task AddAsync(
        Veterinarian veterinarian,
        CancellationToken cancellationToken = default)
        => await _context.Set<Veterinarian>()
            .AddAsync(veterinarian, cancellationToken);

    public Task UpdateAsync(
        Veterinarian veterinarian,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Veterinarian>().Update(veterinarian);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Veterinarian veterinarian,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Veterinarian>().Remove(veterinarian);
        return Task.CompletedTask;
    }
}
