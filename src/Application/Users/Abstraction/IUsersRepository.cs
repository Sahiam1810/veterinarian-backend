using UserEntity = Domain.Users.Entities.Users;

namespace Application.Users.Abstraction;

public interface IUsersRepository
{
    Task<IReadOnlyCollection<UserEntity>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<UserEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task AddAsync(
        UserEntity user,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        UserEntity user,
        CancellationToken cancellationToken);
}
