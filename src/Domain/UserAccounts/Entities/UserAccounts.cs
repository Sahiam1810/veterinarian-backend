using Domain.Common;
using Domain.UserAccounts.ValueObjects;

namespace Domain.UserAccounts.Entities;

public sealed class UserAccounts : BaseEntity<Guid>
{
    private UserAccounts()
    {
    }

    public UserAccounts(Guid userId, string username, string mail, string status)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Username = AccountUsername.Create(username);
        Mail = AccountMail.Create(mail);
        Status = status;
    }

    public Guid UserId { get; private set; }

    public AccountUsername Username { get; private set; } = null!;

    public AccountMail Mail { get; private set; } = null!;

    public string Status { get; private set; } = null!;

    public void Update(string username, string mail, string status)
    {
        Username = AccountUsername.Create(username);
        Mail = AccountMail.Create(mail);
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}
