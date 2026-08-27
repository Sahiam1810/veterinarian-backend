using Domain.AccountStatements.ValueObjects;
using Domain.Common;

namespace Domain.AccountStatements.Entities;

public sealed class AccountStatements : BaseEntity<Guid>
{
    private AccountStatements()
    {
    }

    public AccountStatements(Guid accountId, DateTime issueDate, string status)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        IssueDate = issueDate;
        Status = StatementStatus.Create(status);
    }

    public Guid AccountId { get; private set; }

    public DateTime IssueDate { get; private set; }

    public StatementStatus Status { get; private set; } = null!;

    public void UpdateStatus(string status)
    {
        Status = StatementStatus.Create(status);
        UpdatedAt = DateTime.UtcNow;
    }
}
