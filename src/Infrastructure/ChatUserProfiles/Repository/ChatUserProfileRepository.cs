using Application.ChatUserProfiles.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Infrastructure.ChatUserProfiles.Repository;

public sealed class ChatUserProfileRepository : IChatUserProfileRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatUserProfileRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ChatUserProfileEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatUserProfileEntity>()
            .AsNoTracking()
            .OrderBy(profile => profile.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<ChatUserProfileEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatUserProfileEntity>()
            .FirstOrDefaultAsync(profile => profile.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ChatUserProfileEntity>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatUserProfileEntity>()
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .OrderBy(profile => profile.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        ChatUserProfileEntity profile,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatUserProfileEntity>().AddAsync(profile, cancellationToken);

    public Task UpdateAsync(
        ChatUserProfileEntity profile,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatUserProfileEntity>().Update(profile);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ChatUserProfileEntity profile,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatUserProfileEntity>().Remove(profile);
        return Task.CompletedTask;
    }
}
