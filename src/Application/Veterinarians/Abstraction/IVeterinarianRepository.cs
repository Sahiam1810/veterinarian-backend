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

    Task<bool> ExistsByUserIdAsync(
        Guid userId,
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

    // Id del vet por user, sin navegaciones (para validar citas antes de borrar)
    Task<Guid?> GetIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    // Carga sin Include(User) para poder borrar sin conflicto de tracking
    Task DeleteByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
