using Domain.Veterinarians.Entities;

namespace Application.Veterinarians.Abstraction;

public interface IVeterinarianRepository
{
    Task<IReadOnlyCollection<Veterinarian>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Veterinarian?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<Veterinarian?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> ExistsByLicenseNumberAsync(
        string licenseNumber,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task AddAsync(
        Veterinarian veterinarian,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Veterinarian veterinarian,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Veterinarian veterinarian,
        CancellationToken cancellationToken);
}
