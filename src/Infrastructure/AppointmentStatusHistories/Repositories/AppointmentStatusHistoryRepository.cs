using Application.AppointmentStatusHistories.Abstraction;
using Domain.AppointmentStatusHistories.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AppointmentStatusHistories.Repositories;

public sealed class AppointmentStatusHistoryRepository : IAppointmentStatusHistoryRepository
{
    private readonly VeterinaryDbContext _context;

    public AppointmentStatusHistoryRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<AppointmentStatusHistory>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<AppointmentStatusHistory>()
            .Include(x => x.Appointment)
            .Include(x => x.Status)
            .Include(x => x.ClientPet)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<AppointmentStatusHistory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<AppointmentStatusHistory>()
            .Include(x => x.Appointment)
            .Include(x => x.Status)
            .Include(x => x.ClientPet)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<AppointmentStatusHistory>> GetByAppointmentIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
        => await _context.Set<AppointmentStatusHistory>()
            .AsNoTracking()
            .Where(x => x.AppointmentId == appointmentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        AppointmentStatusHistory appointmentStatusHistory,
        CancellationToken cancellationToken = default)
        => await _context.Set<AppointmentStatusHistory>()
            .AddAsync(appointmentStatusHistory, cancellationToken);

    public Task UpdateAsync(
        AppointmentStatusHistory appointmentStatusHistory,
        CancellationToken cancellationToken = default)
    {
        _context.Set<AppointmentStatusHistory>().Update(appointmentStatusHistory);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        AppointmentStatusHistory appointmentStatusHistory,
        CancellationToken cancellationToken = default)
    {
        _context.Set<AppointmentStatusHistory>().Remove(appointmentStatusHistory);
        return Task.CompletedTask;
    }
}
