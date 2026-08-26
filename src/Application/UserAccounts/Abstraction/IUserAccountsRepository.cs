using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.UserAccounts.Abstraction;

public interface IUserAccountsRepository
{
    Task<IReadOnlyCollection<UserAccountEntity>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<UserAccountEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<UserAccountEntity?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> ExistsByUsernameAsync(
        string username,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task<bool> ExistsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task AddAsync(
        UserAccountEntity account,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        UserAccountEntity account,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        UserAccountEntity account,
        CancellationToken cancellationToken);
}
