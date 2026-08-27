using Application.Availabilities.Abstraction;
using Domain.Availabilities.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Availabilities.Repositories;

public sealed class AvailabilityRepository : IAvailabilityRepository
{
    private readonly VeterinaryDbContext _context;

    public AvailabilityRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Availability>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<Availability>()
            .Include(x => x.Veterinarian)
            .AsNoTracking()
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

    public Task<Availability?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<Availability>()
            .Include(x => x.Veterinarian)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Availability>> GetAllByVeterinarianIdAsync(
        Guid veterinarianId,
        CancellationToken cancellationToken = default)
        => await _context.Set<Availability>()
            .AsNoTracking()
            .Where(x => x.VeterinarianId == veterinarianId)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsOverlapAsync(
        Guid veterinarianId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
        => _context.Set<Availability>()
            .AnyAsync(
                x => x.VeterinarianId == veterinarianId
                    && x.DayOfWeek == dayOfWeek
                    && x.StartTime < endTime
                    && startTime < x.EndTime
                    && (!excludedId.HasValue || x.Id != excludedId.Value),
                cancellationToken);

    public async Task AddAsync(
        Availability availability,
        CancellationToken cancellationToken = default)
        => await _context.Set<Availability>()
            .AddAsync(availability, cancellationToken);

    public Task UpdateAsync(
        Availability availability,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Availability>().Update(availability);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Availability availability,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Availability>().Remove(availability);
        return Task.CompletedTask;
    }
}
