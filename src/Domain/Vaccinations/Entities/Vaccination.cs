using Domain.ClientsPets.Entities;
using Domain.Common;
using Domain.MedicalRecords.Entities;

namespace Domain.Vaccinations.Entities;

public sealed class Vaccination : BaseEntity<Guid>
{
    private Vaccination()
    {
    }

    public Vaccination(
        Guid clientPetId,
        Guid recordId,
        string vaccineName,
        int doseNumber,
        DateTime applicationDate,
        DateTime? nextDoseDate)
    {
        Id = Guid.NewGuid();
        ClientPetId = clientPetId;
        RecordId = recordId;
        VaccineName = vaccineName;
        DoseNumber = doseNumber;
        ApplicationDate = applicationDate;
        NextDoseDate = nextDoseDate;
    }

    public Guid ClientPetId { get; private set; }
    public ClientPetEntity? ClientPet { get; private set; }

    public Guid RecordId { get; private set; }
    public MedicalRecord? Record { get; private set; }

    public string VaccineName { get; private set; } = null!;
    public int DoseNumber { get; private set; }
    public DateTime ApplicationDate { get; private set; }
    public DateTime? NextDoseDate { get; private set; }

    public void Update(
        Guid clientPetId,
        Guid recordId,
        string vaccineName,
        int doseNumber,
        DateTime applicationDate,
        DateTime? nextDoseDate)
    {
        ClientPetId = clientPetId;
        RecordId = recordId;
        VaccineName = vaccineName;
        DoseNumber = doseNumber;
        ApplicationDate = applicationDate;
        NextDoseDate = nextDoseDate;
        UpdatedAt = DateTime.UtcNow;
    }
}
