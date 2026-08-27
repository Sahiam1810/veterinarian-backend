using Domain.Appointments.Entities;
using Domain.ClientsPets.Entities;
using Domain.Common;
using Domain.Diagnostics.Entities;

namespace Domain.MedicalRecords.Entities;

public sealed class MedicalRecord : BaseEntity<Guid>
{
    private MedicalRecord()
    {
    }

    public MedicalRecord(
        Guid clientPetId,
        Guid appointmentId,
        Guid diagnosticId,
        string? symptoms,
        string? treatment,
        decimal? weightAtVisit,
        decimal? temperature)
    {
        Id = Guid.NewGuid();
        ClientPetId = clientPetId;
        AppointmentId = appointmentId;
        DiagnosticId = diagnosticId;
        Symptoms = symptoms;
        Treatment = treatment;
        WeightAtVisit = weightAtVisit;
        Temperature = temperature;
    }

    public Guid ClientPetId { get; private set; }
    public ClientPetEntity? ClientPet { get; private set; }

    public Guid AppointmentId { get; private set; }
    public Appointment? Appointment { get; private set; }

    public Guid DiagnosticId { get; private set; }
    public Diagnostic? Diagnostic { get; private set; }

    public string? Symptoms { get; private set; }
    public string? Treatment { get; private set; }
    public decimal? WeightAtVisit { get; private set; }
    public decimal? Temperature { get; private set; }

    public void Update(
        Guid clientPetId,
        Guid appointmentId,
        Guid diagnosticId,
        string? symptoms,
        string? treatment,
        decimal? weightAtVisit,
        decimal? temperature)
    {
        ClientPetId = clientPetId;
        AppointmentId = appointmentId;
        DiagnosticId = diagnosticId;
        Symptoms = symptoms;
        Treatment = treatment;
        WeightAtVisit = weightAtVisit;
        Temperature = temperature;
        UpdatedAt = DateTime.UtcNow;
    }
}
