using Domain.AiRunStatuses.ValueObjects;
using Domain.Common;

namespace Domain.AiRunStatuses.Entities;

public sealed class AiRunStatusEntity : BaseEntity<Guid>
{
    private AiRunStatusEntity() { }

    public AiRunStatusEntity(string nameStatus)
    {
        Id = Guid.NewGuid();
        NameStatus = AiRunStatusName.Create(nameStatus);
    }

    public AiRunStatusName NameStatus { get; private set; } = null!;

    public void Update(string nameStatus)
    {
        NameStatus = AiRunStatusName.Create(nameStatus);
        UpdatedAt = DateTime.UtcNow;
    }
}
