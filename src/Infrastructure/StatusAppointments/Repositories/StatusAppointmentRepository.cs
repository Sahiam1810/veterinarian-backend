using Application.StatusAppointments.Abstraction;
using Domain.StatusAppointments.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.StatusAppointments.Repositories;

public sealed class StatusAppointmentRepository : IStatusAppointmentRepository
{
    private readonly VeterinaryDbContext _context;

    public StatusAppointmentRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<StatusAppointment>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<StatusAppointment>()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<StatusAppointment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<StatusAppointment>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
        => _context.Set<StatusAppointment>()
            .AnyAsync(
                x => x.Name == name
                    && (!excludedId.HasValue || x.Id != excludedId.Value),
                cancellationToken);

    public async Task AddAsync(
        StatusAppointment statusAppointment,
        CancellationToken cancellationToken = default)
        => await _context.Set<StatusAppointment>()
            .AddAsync(statusAppointment, cancellationToken);

    public Task UpdateAsync(
        StatusAppointment statusAppointment,
        CancellationToken cancellationToken = default)
    {
        _context.Set<StatusAppointment>().Update(statusAppointment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        StatusAppointment statusAppointment,
        CancellationToken cancellationToken = default)
    {
        _context.Set<StatusAppointment>().Remove(statusAppointment);
        return Task.CompletedTask;
    }
}
