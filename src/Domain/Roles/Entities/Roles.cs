using Domain.Common;
using HelpDesk.Domain.Roles.ValueObjects;
using UserEntity = HelpDesk.Domain.Users.Entities.Users;

namespace HelpDesk.Domain.Roles.Entities;

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

    public ICollection<UserEntity> Users { get; } = new List<UserEntity>();

    public void Update(string name, string? description)
    {
        Name = RoleName.Create(name);
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
