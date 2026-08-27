using Application.Notifications.Abstraction;
using Domain.Notifications.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notifications.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly VeterinaryDbContext _context;

    public NotificationRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Notification>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<Notification>()
            .Include(x => x.User)
            .Include(x => x.Appointment)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Notification?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<Notification>()
            .Include(x => x.User)
            .Include(x => x.Appointment)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _context.Set<Notification>()
            .Include(x => x.User)
            .Include(x => x.Appointment)
            .Where(x => x.UserId == userId)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Notification>> GetByAppointmentIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
        => await _context.Set<Notification>()
            .Include(x => x.User)
            .Include(x => x.Appointment)
            .Where(x => x.AppointmentId == appointmentId)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
        => await _context.Set<Notification>()
            .AddAsync(notification, cancellationToken);

    public Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Notification>().Update(notification);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Notification>().Remove(notification);
        return Task.CompletedTask;
    }
}
