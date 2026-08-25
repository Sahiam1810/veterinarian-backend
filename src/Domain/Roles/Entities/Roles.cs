using Domain.Common;
using Domain.Roles.ValueObjects;
// using UserEntity = Domain.Users.Entities.Users; // TODO: habilitar cuando exista la entidad Users

namespace Domain.Roles.Entities;

public sealed class Roles : BaseEntity<Guid>
{
    private Roles()
    {
    }

    public Roles(string name, string? description)
    {
        Id = Guid.NewGuid();
        Name = RoleName.Create(name);
        Description = description;
    }

    public RoleName Name { get; private set; } = null!;

    public string? Description { get; private set; }

    // TODO: habilitar cuando exista la entidad Users
    // public ICollection<UserEntity> Users { get; } = new List<UserEntity>();

    public void Update(string name, string? description)
    {
        Name = RoleName.Create(name);
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
