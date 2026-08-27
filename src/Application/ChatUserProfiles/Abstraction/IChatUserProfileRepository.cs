using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Application.ChatUserProfiles.Abstraction;

public interface IChatUserProfileRepository
{
    Task<IReadOnlyCollection<ChatUserProfileEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ChatUserProfileEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatUserProfileEntity>> GetByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ChatUserProfileEntity profile,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatUserProfileEntity profile,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ChatUserProfileEntity profile,
        CancellationToken cancellationToken = default);
}
