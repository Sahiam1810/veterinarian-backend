using Domain.Common;
using Domain.Users.ValueObjects;

namespace Domain.Users.Entities;

public sealed class Users : BaseEntity<Guid>
{
    private Users()
    {
    }

    public Users(string fullName, string email, string passwordHash, Guid roleId)
    {
        Id = Guid.NewGuid();
        FullName = fullName;
        Email = UserEmail.Create(email);
        PasswordHash = passwordHash;
        RoleId = roleId;
        IsActive = true;
    }

    public string FullName { get; private set; } = null!;

    public UserEmail Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public Guid RoleId { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(string fullName, string email, Guid roleId)
    {
        FullName = fullName;
        Email = UserEmail.Create(email);
        RoleId = roleId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
