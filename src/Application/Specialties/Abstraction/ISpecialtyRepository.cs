using Domain.Specialties.Entities;

namespace Application.Specialties.Abstraction;

public interface ISpecialtyRepository
{
    Task<IReadOnlyCollection<SpecialtyEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<SpecialtyEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken, Guid? excludedId = null);
    Task AddAsync(SpecialtyEntity specialty, CancellationToken cancellationToken);
    Task UpdateAsync(SpecialtyEntity specialty, CancellationToken cancellationToken);
    Task DeleteAsync(SpecialtyEntity specialty, CancellationToken cancellationToken);
}
