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

    // Lookup por teléfono normalizado (solo dígitos); sin índice único aún (2.1).
    Task<ClientEntity?> GetByPhoneAsync(
        string phoneNumber,
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
