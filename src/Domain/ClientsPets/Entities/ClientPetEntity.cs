using Domain.Clients.Entities;
using Domain.ClientsPets.ValueObjects;
using Domain.Common;
using Domain.Pets.Entities;

namespace Domain.ClientsPets.Entities;

public sealed class ClientPetEntity : BaseEntity<Guid>
{
    private ClientPetEntity() { }

    public ClientPetEntity(ClientEntity client, PetEntity pet, bool isPrimaryOwner)
    {
        Id = Guid.NewGuid();
        ClientId = client.Id;
        PetId = pet.Id;
        IsPrimaryOwner = PrimaryOwner.Create(isPrimaryOwner);
    }

    public Guid ClientId { get; private set; }
    public Guid PetId { get; private set; }
    public PrimaryOwner IsPrimaryOwner { get; private set; } = null!;
    public ClientEntity Client { get; private set; } = null!;
    public PetEntity Pet { get; private set; } = null!;

    public void Update(bool isPrimaryOwner)
    {
        IsPrimaryOwner = PrimaryOwner.Create(isPrimaryOwner);
        UpdatedAt = DateTime.UtcNow;
    }
}
