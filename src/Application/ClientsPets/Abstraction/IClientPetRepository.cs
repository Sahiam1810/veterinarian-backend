using Domain.ClientsPets.Entities;

namespace Application.ClientsPets.Abstraction;

public interface IClientPetRepository
{
    Task<IReadOnlyCollection<ClientPetEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<ClientPetEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByClientAndPetAsync(Guid clientId, Guid petId, CancellationToken cancellationToken, Guid? excludedId = null);
    Task AddAsync(ClientPetEntity clientPet, CancellationToken cancellationToken);
    Task UpdateAsync(ClientPetEntity clientPet, CancellationToken cancellationToken);
    Task DeleteAsync(ClientPetEntity clientPet, CancellationToken cancellationToken);
}
