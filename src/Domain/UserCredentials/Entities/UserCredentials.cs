using Domain.Common;

namespace Domain.UserCredentials.Entities;

public sealed class UserCredentials : BaseEntity<Guid>
{
    private UserCredentials()
    {
    }

    public UserCredentials(Guid accountId, string passwordHash)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        PasswordHash = passwordHash;
        LastChanged = DateTime.UtcNow;
    }

    public Guid AccountId { get; private set; }

    public string PasswordHash { get; private set; } = null!;

    public DateTime LastChanged { get; private set; }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        LastChanged = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
