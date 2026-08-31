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

    public async Task<IReadOnlyCollection<Appointment>> GetByClientPetIdsAsync(
        IReadOnlyCollection<Guid> clientPetIds,
        CancellationToken cancellationToken = default)
        => await _context.Set<Appointment>()
            .Include(x => x.ClientPet)
            .Include(x => x.Veterinarian)
            .Include(x => x.Service)
            .Include(x => x.Status)
            .Include(x => x.Availability)
            .Where(x => clientPetIds.Contains(x.ClientPetId))
            .AsNoTracking()
            .OrderByDescending(x => x.ScheduledStart)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Appointment>> GetScheduledBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
        => await _context.Set<Appointment>()
            .Include(x => x.ClientPet!)
                .ThenInclude(x => x.Client)
            .Include(x => x.ClientPet!)
                .ThenInclude(x => x.Pet)
            .Include(x => x.Status)
            .Where(x => x.ScheduledStart >= fromUtc && x.ScheduledStart <= toUtc)
            .AsNoTracking()
            .OrderBy(x => x.ScheduledStart)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasOverlappingAppointmentAsync(
        Guid clientPetId,
        Guid veterinarianId,
        DateTime start,
        DateTime end,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Appointment>().AsQueryable();

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(x => x.Id != excludeAppointmentId.Value);
        }

        return await query.AnyAsync(
            x => (x.ClientPetId == clientPetId || x.VeterinarianId == veterinarianId)
                 && x.ScheduledStart < end
                 && x.ScheduledEnd > start,
            cancellationToken);
    }

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
