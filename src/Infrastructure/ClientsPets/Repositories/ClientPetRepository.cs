using Application.ClientsPets.Abstraction;
using Domain.ClientsPets.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ClientsPets.Repositories;

public sealed class ClientPetRepository(VeterinaryDbContext context) : IClientPetRepository
{
    public async Task<IReadOnlyCollection<ClientPetEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Set<ClientPetEntity>().AsNoTracking().OrderBy(x => x.ClientId).ThenBy(x => x.PetId).ToListAsync(cancellationToken);
    public Task<ClientPetEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => context.Set<ClientPetEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<bool> ExistsByClientAndPetAsync(Guid clientId, Guid petId, CancellationToken cancellationToken, Guid? excludedId = null) =>
        context.Set<ClientPetEntity>().AnyAsync(x => x.ClientId == clientId && x.PetId == petId && (!excludedId.HasValue || x.Id != excludedId.Value), cancellationToken);
    public async Task AddAsync(ClientPetEntity clientPet, CancellationToken cancellationToken) => await context.Set<ClientPetEntity>().AddAsync(clientPet, cancellationToken);
    public Task UpdateAsync(ClientPetEntity clientPet, CancellationToken cancellationToken) { context.Set<ClientPetEntity>().Update(clientPet); return Task.CompletedTask; }
    public Task DeleteAsync(ClientPetEntity clientPet, CancellationToken cancellationToken) { context.Set<ClientPetEntity>().Remove(clientPet); return Task.CompletedTask; }
}
