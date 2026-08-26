using Domain.Common;
using Domain.Specialties.Entities;
using UserEntity = Domain.Users.Entities.Users;

namespace Domain.Veterinarians.Entities;

public sealed class Veterinarian : BaseEntity<Guid>
{
    private Veterinarian()
    {
    }

    public Veterinarian(
        Guid userId,
        Guid specialtyId,
        string licenseNumber)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        SpecialtyId = specialtyId;
        LicenseNumber = licenseNumber;
    }

    public Guid UserId { get; private set; }
    public UserEntity? User { get; private set; }

    public Guid SpecialtyId { get; private set; }
    public SpecialtyEntity? Specialty { get; private set; }

    public string LicenseNumber { get; private set; } = null!;

    public void Update(
        Guid userId,
        Guid specialtyId,
        string licenseNumber)
    {
        UserId = userId;
        SpecialtyId = specialtyId;
        LicenseNumber = licenseNumber;
        UpdatedAt = DateTime.UtcNow;
    }
}
