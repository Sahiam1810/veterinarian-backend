using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;

namespace Application.UserCredentials.Abstraction;

public interface IUserCredentialsRepository
{
    Task<UserCredentialsEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<UserCredentialsEntity?> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<bool> ExistsByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task AddAsync(
        UserCredentialsEntity credentials,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        UserCredentialsEntity credentials,
        CancellationToken cancellationToken);
}
