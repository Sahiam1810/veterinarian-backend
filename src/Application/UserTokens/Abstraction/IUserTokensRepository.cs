using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.UserTokens.Abstraction;

public interface IUserTokensRepository
{
    Task<UserTokenEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<UserTokenEntity?> GetByTokenValueAsync(
        string tokenValue,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserTokenEntity>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task AddAsync(
        UserTokenEntity token,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        UserTokenEntity token,
        CancellationToken cancellationToken);
}
