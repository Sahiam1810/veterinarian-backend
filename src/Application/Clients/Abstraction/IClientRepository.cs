using Domain.Clients.Entities;

namespace Application.Clients.Abstraction;

public interface IClientRepository
{
    Task<IReadOnlyCollection<ClientEntity>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<ClientEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ClientEntity?> GetByIdentificationNumberAsync(
        string identificationNumber,
        CancellationToken cancellationToken);

    Task<ClientEntity?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> ExistsByIdentificationNumberAsync(
        string identificationNumber,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task<bool> ExistsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    // normalizedPhoneNumber: dígitos ya normalizados por ClientPhoneNumber.
    Task<bool> ExistsByPhoneAsync(
        string normalizedPhoneNumber,
        Guid? excludingClientId,
        CancellationToken cancellationToken);

    Task AddAsync(
        ClientEntity client,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        ClientEntity client,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        ClientEntity client,
        CancellationToken cancellationToken);
}
