using Application.Appointments.Abstraction;
using Domain.Appointments.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Appointments.Repositories;

public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly VeterinaryDbContext _context;

    public AppointmentRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Appointment>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<Appointment>()
            .Include(x => x.ClientPet)
            .Include(x => x.Veterinarian)
            .Include(x => x.Service)
            .Include(x => x.Status)
            .Include(x => x.Availability)
            .AsNoTracking()
            .OrderByDescending(x => x.ScheduledStart)
            .ToListAsync(cancellationToken);

    public Task<Appointment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<Appointment>()
            .Include(x => x.ClientPet)
            .Include(x => x.Veterinarian)
            .Include(x => x.Service)
            .Include(x => x.Status)
            .Include(x => x.Availability)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(
        Appointment appointment,
        CancellationToken cancellationToken = default)
        => await _context.Set<Appointment>()
            .AddAsync(appointment, cancellationToken);

    public Task UpdateAsync(
        Appointment appointment,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Appointment>().Update(appointment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Appointment appointment,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Appointment>().Remove(appointment);
        return Task.CompletedTask;
    }
}
